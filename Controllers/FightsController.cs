using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RpgApi.Data;
using RpgApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace RpgApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FightsController : ControllerBase
{
    private readonly RpgContext _context;
    private readonly ILogger<FightsController> _logger;
    private readonly DamageCurveConfig _damageConfig;

    private static readonly TimeSpan StaleInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);

    public FightsController(RpgContext context, ILogger<FightsController> logger, IOptions<DamageCurveConfig> options)
    {
        _context = context;
        _logger = logger;
        _damageConfig = options.Value;
    }

    [HttpPost("/api/v1/fights/start")]
    public async Task<ActionResult<ApiEnvelope<FightSummaryDto>>> StartFightV1([FromBody] StartFightDto dto)
    {
        var idemKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var cacheKey = BuildIdempotencyKey(nameof(StartFightV1), idemKey ?? string.Empty, dto.PlayerId, dto.CharacterId, dto.EnemyId ?? 0);
        if (TryGetIdempotentResponse<ApiEnvelope<FightSummaryDto>>(cacheKey, out var cached))
            return cached!;

        var result = await StartFight(dto);
        var envelopeResult = ToEnvelopeResult(result);
        StoreIdempotentResponse(cacheKey, envelopeResult);
        return envelopeResult;
    }

    [HttpGet("/api/v1/fights/{id}")]
    public async Task<ActionResult<ApiEnvelope<FightSummaryDto>>> GetSessionV1(Guid id)
    {
        var result = await GetSession(id);
        return ToEnvelopeResult(result);
    }

    [HttpPost("/api/v1/fights/{id}/turn")]
    public async Task<ActionResult<ApiEnvelope<FightSummaryDto>>> TakeTurnV1(Guid id, [FromBody] FightTurnV1Dto dto)
    {
        var session = await _context.FightSessions.FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            return NotFound(EnvelopeError<FightSummaryDto>("fight_not_found", "Fight session not found"));

        if (dto.ExpectedTurn.HasValue && dto.ExpectedTurn.Value != session.CurrentTurn)
            return Conflict(EnvelopeError<FightSummaryDto>(
                "turn_mismatch",
                $"Expected turn {dto.ExpectedTurn.Value} but server is on turn {session.CurrentTurn}"));

        var idemKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var cacheKey = BuildIdempotencyKey(nameof(TakeTurnV1), idemKey ?? string.Empty, id.ToString(), dto.PlayerId, dto.SkillId, dto.ExpectedTurn ?? 0);
        if (TryGetIdempotentResponse<ApiEnvelope<FightSummaryDto>>(cacheKey, out var cached))
            return cached!;

        var result = await TakeTurn(id, new FightTurnDto(dto.PlayerId, dto.SkillId));
        var envelopeResult = ToEnvelopeResult(result);
        StoreIdempotentResponse(cacheKey, envelopeResult);
        return envelopeResult;
    }

    [HttpPost("/api/v1/fights/{id}/passive-skill/activate")]
    public async Task<ActionResult<ApiEnvelope<FightSummaryDto>>> ActivatePassiveSkillV1(Guid id, [FromBody] ActivatePassiveSkillDto dto)
    {
        var result = await ActivatePassiveSkill(id, dto);
        return ToEnvelopeResult(result);
    }

    [HttpPost("/api/v1/fights/{id}/passive-skill/deactivate")]
    public async Task<ActionResult<ApiEnvelope<FightSummaryDto>>> DeactivatePassiveSkillV1(Guid id, [FromBody] DeactivatePassiveSkillDto dto)
    {
        var result = await DeactivatePassiveSkill(id, dto);
        return ToEnvelopeResult(result);
    }

    [HttpPost("/api/v1/fights/{id}/actions/preview")]
    public async Task<ActionResult<ApiEnvelope<FightActionPreviewDto>>> PreviewActionV1(Guid id, [FromBody] FightActionPreviewRequest dto)
    {
        var session = await _context.FightSessions.FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            return NotFound(EnvelopeError<FightActionPreviewDto>("fight_not_found", "Fight session not found"));

        if (session.Status != FightStatus.InProgress && session.Status != FightStatus.Waiting)
            return BadRequest(EnvelopeError<FightActionPreviewDto>("fight_inactive", "Fight is not currently active"));

        if (session.Status == FightStatus.Stale || session.LastActionAt.Add(StaleInterval) < DateTime.UtcNow)
            return Conflict(EnvelopeError<FightActionPreviewDto>("fight_stale", "Fight session is stale"));

        if (session.PlayerId != dto.PlayerId)
            return BadRequest(EnvelopeError<FightActionPreviewDto>("invalid_player", "Player is not part of this fight session"));

        var character = await _context.Characters
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == session.CharacterId);
        if (character == null)
            return NotFound(EnvelopeError<FightActionPreviewDto>("character_not_found", "Character not found"));

        var skill = character.Skills.FirstOrDefault(s => s.Id == dto.SkillId);
        if (skill == null)
            return BadRequest(EnvelopeError<FightActionPreviewDto>("invalid_skill", "Invalid skill"));

        var reasons = new List<string>();
        var characterCooldowns = session.GetCharacterCooldowns();
        var cooldownRemaining = characterCooldowns.TryGetValue(skill.Id, out var cd) ? cd : 0;

        if (character.Level < skill.RequiredLevel)
            reasons.Add($"Requires level {skill.RequiredLevel}");
        if (cooldownRemaining > 0)
            reasons.Add($"Skill is on cooldown for {cooldownRemaining} more turn(s)");

        var hasEnoughMana = session.CharacterCurrentMana >= skill.ManaCost;
        var hasEnoughStamina = session.CharacterCurrentStamina >= skill.StaminaCost;
        if (!hasEnoughMana)
            reasons.Add($"Not enough mana ({session.CharacterCurrentMana}/{skill.ManaCost})");
        if (!hasEnoughStamina)
            reasons.Add($"Not enough stamina ({session.CharacterCurrentStamina}/{skill.StaminaCost})");

        var manaAfterAction = Math.Max(0, session.CharacterCurrentMana - skill.ManaCost);
        var staminaAfterAction = Math.Max(0, session.CharacterCurrentStamina - skill.StaminaCost);

        var passiveInteractions = BuildPassivePreviewInteractions(session, character, manaAfterAction, staminaAfterAction);
        var passivesThatDrop = passiveInteractions.Where(p => p.Active && !p.WillRemainActive).ToList();
        if (passivesThatDrop.Count > 0)
        {
            var droppedNames = string.Join(", ", passivesThatDrop.Select(p => p.Name));
            reasons.Add($"Will deactivate passives this turn: {droppedNames}");
        }

        var canUse = reasons.Count == 0;
        var preview = new FightActionPreviewDto(
            canUse,
            skill.Id,
            skill.Name,
            cooldownRemaining,
            hasEnoughMana,
            hasEnoughStamina,
            manaAfterAction,
            staminaAfterAction,
            passiveInteractions,
            reasons);

        return Ok(Envelope(preview));
    }

    [HttpPost("/api/v1/fights/simulate")]
    public async Task<ActionResult<ApiEnvelope<FightSimulationResultDto>>> SimulateFightV1([FromBody] FightSimulationRequest dto)
    {
        var character = await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Class)
            .FirstOrDefaultAsync(c => c.Id == dto.CharacterId && c.PlayerId == dto.PlayerId);
        if (character == null)
            return NotFound(EnvelopeError<FightSimulationResultDto>("character_not_found", "Character not found for player"));

        var enemy = await _context.Enemies
            .Include(e => e.Skills)
            .Include(e => e.EnemyClass)
            .FirstOrDefaultAsync(e => e.Id == dto.EnemyId);
        if (enemy == null)
            return NotFound(EnvelopeError<FightSimulationResultDto>("enemy_not_found", "Enemy not found"));

        var random = new Random(dto.Seed);
        var characterHp = character.Health;
        var enemyHp = enemy.Health;
        var characterMana = character.Mana;
        var characterStamina = character.Stamina;
        var enemyMana = enemy.Mana;
        var enemyStamina = enemy.Stamina;
        var turn = 1;
        var log = new List<FightSimulationTurnDto>();
        var characterCooldowns = character.Skills.ToDictionary(s => s.Id, _ => 0);
        var enemyCooldowns = enemy.Skills.ToDictionary(s => s.Id, _ => 0);

        while (turn <= dto.MaxTurns && characterHp > 0 && enemyHp > 0)
        {
            var playerSkill = character.Skills
                .Where(s => s.RequiredLevel <= character.Level)
                .Where(s => characterCooldowns.TryGetValue(s.Id, out var c) && c == 0)
                .Where(s => characterMana >= s.ManaCost && characterStamina >= s.StaminaCost)
                .OrderBy(s => s.Id)
                .FirstOrDefault();

            int playerDamage = 0;
            string playerAction;
            if (playerSkill != null)
            {
                characterMana -= playerSkill.ManaCost;
                characterStamina -= playerSkill.StaminaCost;
                characterCooldowns[playerSkill.Id] = playerSkill.Cooldown;

                var charPower = character.Attack * _damageConfig.CharacterAttackWeight
                                + character.Magic * _damageConfig.CharacterMagicWeight
                                + character.Speed * _damageConfig.CharacterSpeedWeight;
                var skillPower = playerSkill.AttackPower * _damageConfig.SkillAttackWeight
                               + playerSkill.MagicPower * _damageConfig.SkillMagicWeight
                               + playerSkill.SpeedModifier * _damageConfig.SkillSpeedWeight;
                var enemyDefense = enemy.Defense * _damageConfig.EnemyDefenseWeight
                                 + enemy.Magic * _damageConfig.EnemyMagicDefenseWeight
                                 + enemy.Stamina * _damageConfig.EnemyStaminaDefenseWeight;
                var elementMultiplier = GetResistanceMultiplier(playerSkill.Element, enemy.EnemyClass);
                playerDamage = (int)Math.Max(_damageConfig.MinimumDamage,
                    Math.Floor((charPower + (skillPower * playerSkill.ElementPowerMultiplier) - enemyDefense) * elementMultiplier));
                playerDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(playerDamage * (0.9 + random.NextDouble() * 0.2)));
                playerAction = $"{playerSkill.Name} ({playerSkill.Element})";
            }
            else
            {
                var charPower = character.Attack * _damageConfig.CharacterAttackWeight;
                var enemyDefense = enemy.Defense * _damageConfig.EnemyDefenseWeight;
                playerDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(charPower - enemyDefense * 0.5));
                playerAction = "Basic attack";
            }

            enemyHp = Math.Max(0, enemyHp - playerDamage);
            if (enemyHp <= 0)
            {
                log.Add(new FightSimulationTurnDto(turn, playerAction, playerDamage, "Enemy defeated", 0, characterHp, enemyHp));
                break;
            }

            DecrementCooldowns(characterCooldowns);

            var enemySkill = enemy.Skills
                .Where(s => enemyCooldowns.TryGetValue(s.Id, out var c) && c == 0)
                .Where(s => enemyMana >= s.ManaCost && enemyStamina >= s.StaminaCost)
                .OrderBy(s => s.Id)
                .FirstOrDefault();

            int enemyDamage;
            string enemyAction;
            if (enemySkill != null)
            {
                enemyMana -= enemySkill.ManaCost;
                enemyStamina -= enemySkill.StaminaCost;
                enemyCooldowns[enemySkill.Id] = enemySkill.Cooldown;

                var enemyPower = enemy.Attack * _damageConfig.EnemyAttackWeight
                               + enemy.Magic * _damageConfig.EnemyMagicWeight
                               + enemy.Speed * _damageConfig.EnemySpeedWeight;
                var enemySkillPower = enemySkill.AttackPower * _damageConfig.SkillAttackWeight
                                    + enemySkill.MagicPower * _damageConfig.SkillMagicWeight
                                    + enemySkill.SpeedModifier * _damageConfig.SkillSpeedWeight;
                var charDefense = character.Defense * _damageConfig.CharacterDefenseWeight
                                + character.Magic * _damageConfig.CharacterMagicDefenseWeight
                                + character.Stamina * _damageConfig.CharacterStaminaDefenseWeight;
                var elementMultiplier = GetResistanceMultiplier(enemySkill.Element, character.Class);
                enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage,
                    Math.Floor((enemyPower + (enemySkillPower * enemySkill.ElementPowerMultiplier) - charDefense * 0.5) * elementMultiplier));
                enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(enemyDamage * (0.9 + random.NextDouble() * 0.2)));
                enemyAction = $"{enemySkill.Name} ({enemySkill.Element})";
            }
            else
            {
                var enemyPower = enemy.Attack * _damageConfig.EnemyAttackWeight;
                var charDefense = character.Defense * _damageConfig.CharacterDefenseWeight;
                enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(enemyPower - charDefense * 0.5));
                enemyAction = "Basic attack";
            }

            characterHp = Math.Max(0, characterHp - enemyDamage);
            DecrementCooldowns(enemyCooldowns);

            log.Add(new FightSimulationTurnDto(turn, playerAction, playerDamage, enemyAction, enemyDamage, characterHp, enemyHp));
            turn++;
        }

        var result = new FightSimulationResultDto(
            dto.Seed,
            character.Id,
            enemy.Id,
            characterHp > 0 && enemyHp <= 0,
            log.Count,
            log,
            new FightSimulationAggregateDto(
                log.Sum(t => t.PlayerDamage),
                log.Sum(t => t.EnemyDamage),
                log.Count == 0 ? 0 : log.Average(t => t.PlayerDamage),
                log.Count == 0 ? 0 : log.Average(t => t.EnemyDamage),
                characterMana,
                characterStamina));

        return Ok(Envelope(result));
    }

    [HttpPut("/api/v1/fights/{id}/position")]
    public async Task<ActionResult<ApiEnvelope<FightSummaryDto>>> SetPositionV1(Guid id, [FromBody] SetBattlePositionDto dto)
    {
        var session = await _context.FightSessions.FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            return NotFound(EnvelopeError<FightSummaryDto>("fight_not_found", "Fight session not found"));

        RuntimeStores.CharacterRows[id] = dto.CharacterRow;
        RuntimeStores.EnemyRows[id] = dto.EnemyRow;
        var summary = await BuildSummaryDto(id);
        return Ok(Envelope(summary));
    }

    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<AvailableFightDto>>> GetAvailableFights()
    {
        var now = DateTime.UtcNow;
        var sessions = await _context.FightSessions
            .Where(fs => fs.Status == FightStatus.InProgress && !fs.IsLocked)
            .ToListAsync();

        foreach (var session in sessions)
        {
            if (session.LastActionAt.Add(StaleInterval) < now)
            {
                session.Status = FightStatus.Stale;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(sessions
            .Where(fs => fs.Status == FightStatus.InProgress)
            .Select(fs => new AvailableFightDto(fs.Id, fs.CharacterName, fs.EnemyName, fs.CurrentTurn)));
    }

    [HttpPost("match")]
    public async Task<ActionResult<AvailableFightDto>> CreateMatch([FromBody] CreateMatchDto dto)
    {
        var character = await _context.Characters
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == dto.CharacterId && c.PlayerId == dto.PlayerId);
        if (character == null)
            return NotFound(new { message = "Character not found for player" });

        var enemy = await _context.Enemies
            .Include(e => e.Skills)
            .FirstOrDefaultAsync(e => e.Id == dto.EnemyId);
        if (enemy == null)
            return NotFound(new { message = "Enemy not found" });

        var session = CreateSessionObject(character, enemy, dto.PlayerId);
        session.Status = FightStatus.Waiting;
        await _context.FightSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, new AvailableFightDto(session.Id, session.CharacterName, session.EnemyName, session.CurrentTurn));
    }

    [HttpPost("{id}/lock")]
    public async Task<ActionResult> LockFight(Guid id, [FromBody] LockFightDto dto)
    {
        var session = await _context.FightSessions.FindAsync(id);
        if (session == null)
            return NotFound(new { message = "Fight session not found" });

        if (session.Status != FightStatus.InProgress && session.Status != FightStatus.Waiting)
            return BadRequest(new { message = "Fight cannot be locked in current state" });

        if (session.LastActionAt.Add(StaleInterval) < DateTime.UtcNow)
        {
            session.Status = FightStatus.Stale;
            await _context.SaveChangesAsync();
            return Conflict(new { message = "Fight session is stale" });
        }

        if (session.IsLocked && session.LockedByPlayerId != dto.PlayerId && session.LastActionAt.Add(LockTimeout) > DateTime.UtcNow)
            return Conflict(new { message = "Fight session is locked by another player" });

        session.IsLocked = true;
        session.LockedByPlayerId = dto.PlayerId;
        session.LastActionAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("start")]
    public async Task<ActionResult<FightSummaryDto>> StartFight([FromBody] StartFightDto dto)
    {
        var character = await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Class)
            .FirstOrDefaultAsync(c => c.Id == dto.CharacterId && c.PlayerId == dto.PlayerId);
        if (character == null)
            return NotFound(new { message = "Character not found for player" });

        Enemy enemy;
        if (dto.EnemyId.HasValue)
        {
            var en = await _context.Enemies
                .Include(e => e.Skills)
                .Include(e => e.EnemyClass)
                .FirstOrDefaultAsync(e => e.Id == dto.EnemyId.Value);
            if (en == null)
                return NotFound(new { message = "Enemy not found" });
            enemy = en;
        }
        else
        {
            var list = await _context.Enemies
                .Include(e => e.Skills)
                .Include(e => e.EnemyClass)
                .ToListAsync();
            if (!list.Any())
                return BadRequest(new { message = "No enemies available" });
            var random = new Random();
            enemy = list[random.Next(list.Count)];
        }

        var session = CreateSessionObject(character, enemy, dto.PlayerId);
        session.Status = FightStatus.InProgress;

        _context.FightSessions.Add(session);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, await BuildSummaryDto(session.Id));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FightSummaryDto>> GetSession(Guid id)
    {
        var session = await _context.FightSessions
            .Include(fs => fs.Moves)
            .FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            return NotFound(new { message = "Fight session not found" });

        if (session.Status == FightStatus.InProgress && session.LastActionAt.Add(StaleInterval) < DateTime.UtcNow)
        {
            session.Status = FightStatus.Stale;
            await _context.SaveChangesAsync();
        }

        return Ok(await BuildSummaryDto(id));
    }

    [HttpPost("{id}/passive-skill/activate")]
    public async Task<ActionResult<FightSummaryDto>> ActivatePassiveSkill(Guid id, [FromBody] ActivatePassiveSkillDto dto)
    {
        var session = await _context.FightSessions
            .Include(fs => fs.Moves)
            .FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            return NotFound(new { message = "Fight session not found" });

        if (session.Status != FightStatus.InProgress && session.Status != FightStatus.Waiting)
            return BadRequest(new { message = "Fight is not currently active" });

        var character = await _context.Characters
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == session.CharacterId);
        if (character == null)
            return NotFound(new { message = "Character not found" });

        var skill = character.Skills.FirstOrDefault(s => s.Id == dto.SkillId);
        if (skill == null)
            return BadRequest(new { message = "Invalid skill" });

        if (skill.Type != SkillType.Passive)
            return BadRequest(new { message = "Skill must be passive to activate" });

        if (character.Level < skill.RequiredLevel)
            return BadRequest(new { message = $"Character must be level {skill.RequiredLevel} or higher" });

        var activePassives = session.GetCharacterActivePassiveSkills();
        if (!activePassives.Contains(skill.Id))
        {
            activePassives.Add(skill.Id);
            session.SetCharacterActivePassiveSkills(activePassives);
            await _context.SaveChangesAsync();
        }

        return Ok(await BuildSummaryDto(session.Id));
    }

    [HttpPost("{id}/passive-skill/deactivate")]
    public async Task<ActionResult<FightSummaryDto>> DeactivatePassiveSkill(Guid id, [FromBody] DeactivatePassiveSkillDto dto)
    {
        var session = await _context.FightSessions
            .Include(fs => fs.Moves)
            .FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            return NotFound(new { message = "Fight session not found" });

        var activePassives = session.GetCharacterActivePassiveSkills();
        if (activePassives.Contains(dto.SkillId))
        {
            activePassives.Remove(dto.SkillId);
            session.SetCharacterActivePassiveSkills(activePassives);
            await _context.SaveChangesAsync();
        }

        return Ok(await BuildSummaryDto(session.Id));
    }

    [HttpGet("curve")]
    public ActionResult<DamageCurveConfig> GetDamageCurveConfig()
    {
        return Ok(_damageConfig);
    }

    [HttpPut("curve")]
    public ActionResult SetDamageCurveConfig([FromBody] DamageCurveConfig settings)
    {
        // For now this update only affects in-memory config for the current process.
        // In production, persist to appsettings or database as needed.
        _damageConfig.CharacterAttackWeight = settings.CharacterAttackWeight;
        _damageConfig.CharacterMagicWeight = settings.CharacterMagicWeight;
        _damageConfig.CharacterSpeedWeight = settings.CharacterSpeedWeight;
        _damageConfig.SkillAttackWeight = settings.SkillAttackWeight;
        _damageConfig.SkillMagicWeight = settings.SkillMagicWeight;
        _damageConfig.SkillSpeedWeight = settings.SkillSpeedWeight;
        _damageConfig.EnemyDefenseWeight = settings.EnemyDefenseWeight;
        _damageConfig.EnemyMagicDefenseWeight = settings.EnemyMagicDefenseWeight;
        _damageConfig.EnemyStaminaDefenseWeight = settings.EnemyStaminaDefenseWeight;
        _damageConfig.EnemyAttackWeight = settings.EnemyAttackWeight;
        _damageConfig.EnemyMagicWeight = settings.EnemyMagicWeight;
        _damageConfig.EnemySpeedWeight = settings.EnemySpeedWeight;
        _damageConfig.CharacterDefenseWeight = settings.CharacterDefenseWeight;
        _damageConfig.CharacterMagicDefenseWeight = settings.CharacterMagicDefenseWeight;
        _damageConfig.CharacterStaminaDefenseWeight = settings.CharacterStaminaDefenseWeight;
        _damageConfig.MinimumDamage = settings.MinimumDamage;

        return NoContent();
    }

    [HttpPost("{id}/turn")]
    public async Task<ActionResult<FightSummaryDto>> TakeTurn(Guid id, [FromBody] FightTurnDto dto)
    {
        var session = await _context.FightSessions
            .Include(fs => fs.Moves)
            .FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            return NotFound(new { message = "Fight session not found" });

        if (session.Status != FightStatus.InProgress && session.Status != FightStatus.Waiting)
            return BadRequest(new { message = "Fight is not currently active" });

        if (session.Status == FightStatus.Stale || session.LastActionAt.Add(StaleInterval) < DateTime.UtcNow)
        {
            session.Status = FightStatus.Stale;
            await _context.SaveChangesAsync();
            return Conflict(new { message = "Fight session is stale" });
        }

        if (session.IsLocked && session.LockedByPlayerId != dto.PlayerId && session.LastActionAt.Add(LockTimeout) > DateTime.UtcNow)
            return Conflict(new { message = "Fight session is locked by another player" });

        var character = await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Class)
            .FirstOrDefaultAsync(c => c.Id == session.CharacterId);
        if (character == null)
            return NotFound(new { message = "Character not found" });

        var enemy = await _context.Enemies
            .Include(e => e.Skills)
            .Include(e => e.EnemyClass)
            .FirstOrDefaultAsync(e => e.Id == session.EnemyId);
        if (enemy == null)
            return NotFound(new { message = "Enemy not found" });

        var characterCooldowns = session.GetCharacterCooldowns();
        var enemyCooldowns = session.GetEnemyCooldowns();

        var random = new Random();
        var characterRow = RuntimeStores.CharacterRows.TryGetValue(session.Id, out var storedCharacterRow) ? storedCharacterRow : BattleRow.Mid;
        var enemyRow = RuntimeStores.EnemyRows.TryGetValue(session.Id, out var storedEnemyRow) ? storedEnemyRow : BattleRow.Mid;
        var characterEffects = RuntimeStores.CharacterStatusEffects.TryGetValue(session.Id, out var storedCharacterEffects)
            ? storedCharacterEffects
            : new List<StatusEffectState>();
        var enemyEffects = RuntimeStores.EnemyStatusEffects.TryGetValue(session.Id, out var storedEnemyEffects)
            ? storedEnemyEffects
            : new List<StatusEffectState>();

        var playerStatus = ApplyStatusEffects(characterEffects);
        characterEffects = playerStatus.UpdatedEffects;
        if (playerStatus.Damage > 0)
        {
            session.CharacterCurrentHp = Math.Max(0, session.CharacterCurrentHp - playerStatus.Damage);
            session.Moves.Add(new FightMove
            {
                FightSessionId = session.Id,
                Turn = session.CurrentTurn,
                IsPlayer = true,
                SkillId = 0,
                Damage = playerStatus.Damage,
                Description = $"Character suffers {playerStatus.Damage} damage from status effects."
            });
        }

        if (playerStatus.SkipAction)
        {
            RuntimeStores.CharacterStatusEffects[session.Id] = characterEffects;
            RuntimeStores.EnemyStatusEffects[session.Id] = enemyEffects;
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "Character is frozen or stunned and cannot act this turn" });
        }

        var playerSkill = character.Skills.FirstOrDefault(s => s.Id == dto.SkillId);
        if (playerSkill == null)
            return BadRequest(new { message = "Invalid skill" });

        if (character.Level < playerSkill.RequiredLevel)
            return BadRequest(new { message = $"Character must be level {playerSkill.RequiredLevel} or higher to use {playerSkill.Name}" });

        if (!characterCooldowns.TryGetValue(playerSkill.Id, out var cd) || cd > 0)
            return BadRequest(new { message = "Skill is on cooldown" });

        if (playerSkill.ManaCost > 0 && session.CharacterCurrentMana < playerSkill.ManaCost)
            return BadRequest(new { message = $"Not enough mana to use {playerSkill.Name} (costs {playerSkill.ManaCost})" });
        if (playerSkill.StaminaCost > 0 && session.CharacterCurrentStamina < playerSkill.StaminaCost)
            return BadRequest(new { message = $"Not enough stamina to use {playerSkill.Name} (costs {playerSkill.StaminaCost})" });

        session.CharacterCurrentMana -= playerSkill.ManaCost;
        session.CharacterCurrentStamina -= playerSkill.StaminaCost;

        var charPower = character.Attack * _damageConfig.CharacterAttackWeight
                        + character.Magic * _damageConfig.CharacterMagicWeight
                        + character.Speed * _damageConfig.CharacterSpeedWeight;
        var skillPower = playerSkill.AttackPower * _damageConfig.SkillAttackWeight
                       + playerSkill.MagicPower * _damageConfig.SkillMagicWeight
                       + playerSkill.SpeedModifier * _damageConfig.SkillSpeedWeight;
        var charDefense = enemy.Defense * _damageConfig.EnemyDefenseWeight
                        + enemy.Magic * _damageConfig.EnemyMagicDefenseWeight
                        + enemy.Stamina * _damageConfig.EnemyStaminaDefenseWeight;
        if (HasEffect(enemyEffects, StatusEffectType.ShieldBreak))
            charDefense *= 0.8;

        var playerElementMultiplier = GetResistanceMultiplier(playerSkill.Element, enemy.EnemyClass);
        var playerEffectiveSkillPower = skillPower * playerSkill.ElementPowerMultiplier;
        var playerEffectiveness = DescribeElementEffect(playerElementMultiplier);

        var comboMultiplier = 1.0;
        var priorPlayerMove = session.Moves
            .Where(m => m.IsPlayer && m.SkillId > 0)
            .OrderByDescending(m => m.Turn)
            .FirstOrDefault();
        if (priorPlayerMove != null)
        {
            var priorSkill = character.Skills.FirstOrDefault(s => s.Id == priorPlayerMove.SkillId);
            if (priorSkill != null && priorSkill.Element == playerSkill.Element)
                comboMultiplier = 1.1;
        }

        var rowDamageMultiplier = GetRowDamageMultiplier(characterRow, enemyRow);

        var basePlDamage = Math.Max(_damageConfig.MinimumDamage,
            (int)Math.Floor((charPower + playerEffectiveSkillPower - charDefense) * playerElementMultiplier));
        var plDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(basePlDamage * comboMultiplier * rowDamageMultiplier * (0.9 + random.NextDouble() * 0.2)));
        session.EnemyCurrentHp -= plDamage;
        session.Moves.Add(new FightMove
        {
            FightSessionId = session.Id,
            Turn = session.CurrentTurn,
            IsPlayer = true,
            SkillId = playerSkill.Id,
            Damage = plDamage,
            Description = $"Character uses {playerSkill.Name} [{playerSkill.Element}] ({playerEffectiveness}); deals {plDamage} damage."
        });

        enemyEffects = MergeEffects(enemyEffects, BuildInflictedEffects(playerSkill));

        characterCooldowns[playerSkill.Id] = playerSkill.Cooldown;

        if (session.EnemyCurrentHp <= 0)
        {
            session.EnemyCurrentHp = 0;
            session.Status = FightStatus.Completed;
            session.IsVictory = true;
            session.LastActionAt = DateTime.UtcNow;
            await GrantExperience(character, enemy.ExperienceReward);
            AddProgressionEvent(
                "fight_completed",
                $"{character.Name} defeated {enemy.Name}",
                character.PlayerId,
                character.Id,
                $"sessionId={session.Id};enemyId={enemy.Id}");
            session.SetCharacterCooldowns(characterCooldowns);
            await _context.SaveChangesAsync();
            return Ok(await BuildSummaryDto(session.Id));
        }

        DecrementCooldowns(characterCooldowns);

        var enemySkill = enemy.Skills
            .Where(s => enemyCooldowns.TryGetValue(s.Id, out var c2) && c2 == 0)
            .OrderBy(_ => Guid.NewGuid())
            .FirstOrDefault();

        int enemyDamage;
        string enemyEvent;

        var enemyPower = enemy.Attack * _damageConfig.EnemyAttackWeight
                        + enemy.Magic * _damageConfig.EnemyMagicWeight
                        + enemy.Speed * _damageConfig.EnemySpeedWeight;
        var enemyDefense = character.Defense * _damageConfig.CharacterDefenseWeight
                         + character.Magic * _damageConfig.CharacterMagicDefenseWeight
                         + character.Stamina * _damageConfig.CharacterStaminaDefenseWeight;
        if (HasEffect(characterEffects, StatusEffectType.ShieldBreak))
            enemyDefense *= 0.8;

        var enemyStatus = ApplyStatusEffects(enemyEffects);
        enemyEffects = enemyStatus.UpdatedEffects;
        if (enemyStatus.Damage > 0)
        {
            session.EnemyCurrentHp = Math.Max(0, session.EnemyCurrentHp - enemyStatus.Damage);
            session.Moves.Add(new FightMove
            {
                FightSessionId = session.Id,
                Turn = session.CurrentTurn,
                IsPlayer = false,
                SkillId = 0,
                Damage = enemyStatus.Damage,
                Description = $"Enemy suffers {enemyStatus.Damage} damage from status effects."
            });

            if (session.EnemyCurrentHp <= 0)
            {
                session.Status = FightStatus.Completed;
                session.IsVictory = true;
                session.LastActionAt = DateTime.UtcNow;
                await GrantExperience(character, enemy.ExperienceReward);
                AddProgressionEvent(
                    "fight_completed",
                    $"{character.Name} defeated {enemy.Name}",
                    character.PlayerId,
                    character.Id,
                    $"sessionId={session.Id};enemyId={enemy.Id}");
                RuntimeStores.CharacterStatusEffects[session.Id] = characterEffects;
                RuntimeStores.EnemyStatusEffects[session.Id] = enemyEffects;
                await _context.SaveChangesAsync();
                return Ok(await BuildSummaryDto(session.Id));
            }
        }

        if (enemySkill != null)
        {
            var enemySkillPower = enemySkill.AttackPower * _damageConfig.SkillAttackWeight
                                + enemySkill.MagicPower * _damageConfig.SkillMagicWeight
                                + enemySkill.SpeedModifier * _damageConfig.SkillSpeedWeight;
            var enemyElementMultiplier = GetResistanceMultiplier(enemySkill.Element, character.Class);
            var enemyEffectiveSkillPower = enemySkillPower * enemySkill.ElementPowerMultiplier;
            var enemyEffectiveness = DescribeElementEffect(enemyElementMultiplier);
            enemyCooldowns[enemySkill.Id] = enemySkill.Cooldown;
            var enemyRowDamageMultiplier = GetRowDamageMultiplier(enemyRow, characterRow);
            var baseEnemyDamage = Math.Max(_damageConfig.MinimumDamage,
                (int)Math.Floor((enemyPower + enemyEffectiveSkillPower - enemyDefense * 0.5) * enemyElementMultiplier));
            enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(baseEnemyDamage * enemyRowDamageMultiplier * (0.9 + random.NextDouble() * 0.2)));
            enemyEvent = $"Enemy uses {enemySkill.Name} [{enemySkill.Element}] ({enemyEffectiveness}); deals {enemyDamage} damage.";
            characterEffects = MergeEffects(characterEffects, BuildInflictedEffects(enemySkill));
        }
        else
        {
            var baseEnemyDamage = Math.Max(_damageConfig.MinimumDamage,
                (int)Math.Floor(enemyPower - enemyDefense * 0.5));
            enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(baseEnemyDamage * (0.9 + random.NextDouble() * 0.2)));
            enemyEvent = $"Enemy attacks normally; deals {enemyDamage} damage.";
        }

        if (enemyStatus.SkipAction)
        {
            enemyDamage = 0;
            enemyEvent = "Enemy is frozen or stunned and skips the turn.";
        }

        session.CharacterCurrentHp -= enemyDamage;
        session.Moves.Add(new FightMove
        {
            FightSessionId = session.Id,
            Turn = session.CurrentTurn,
            IsPlayer = false,
            SkillId = enemySkill?.Id ?? 0,
            Damage = enemyDamage,
            Description = enemyEvent
        });

        if (session.CharacterCurrentHp <= 0)
        {
            session.CharacterCurrentHp = 0;
            session.Status = FightStatus.Completed;
            session.IsVictory = false;
            session.LastActionAt = DateTime.UtcNow;
            AddProgressionEvent(
                "fight_completed",
                $"{character.Name} was defeated by {enemy.Name}",
                character.PlayerId,
                character.Id,
                $"sessionId={session.Id};enemyId={enemy.Id}");
            session.SetCharacterCooldowns(characterCooldowns);
            session.SetEnemyCooldowns(enemyCooldowns);
            await _context.SaveChangesAsync();
            return Ok(await BuildSummaryDto(session.Id));
        }

        DecrementCooldowns(enemyCooldowns);
        session.CurrentTurn++;
        session.Status = FightStatus.InProgress;
        session.LastActionAt = DateTime.UtcNow;
        session.SetCharacterCooldowns(characterCooldowns);
        session.SetEnemyCooldowns(enemyCooldowns);

        // Apply passive skill resource consumption
        var characterActivePassives = session.GetCharacterActivePassiveSkills();
        var passivesToRemove = new List<int>();
        foreach (var passiveSkillId in characterActivePassives)
        {
            var passiveSkill = character.Skills.FirstOrDefault(s => s.Id == passiveSkillId);
            if (passiveSkill != null)
            {
                // Check if character has enough resources to maintain the passive
                bool canMaintain = true;
                if (passiveSkill.ManaCost > 0 && session.CharacterCurrentMana < passiveSkill.ManaCost)
                    canMaintain = false;
                if (passiveSkill.StaminaCost > 0 && session.CharacterCurrentStamina < passiveSkill.StaminaCost)
                    canMaintain = false;

                if (!canMaintain)
                {
                    passivesToRemove.Add(passiveSkillId);
                }
                else
                {
                    // Consume resources for passive skill
                    session.CharacterCurrentMana -= passiveSkill.ManaCost;
                    session.CharacterCurrentStamina -= passiveSkill.StaminaCost;
                }
            }
        }
        // Remove passives that couldn't be maintained
        foreach (var skillId in passivesToRemove)
        {
            characterActivePassives.Remove(skillId);
        }
        session.SetCharacterActivePassiveSkills(characterActivePassives);

        // Apply regeneration to both character and enemy
        session.CharacterCurrentHp = Math.Min(session.CharacterCurrentHp + session.CharacterHealthRegen, session.CharacterMaxHp);
        session.CharacterCurrentMana = Math.Min(session.CharacterCurrentMana + session.CharacterManaRegen + GetRowManaRegenBonus(characterRow), session.CharacterMaxMana);
        session.CharacterCurrentStamina = Math.Min(session.CharacterCurrentStamina + session.CharacterStaminaRegen, session.CharacterMaxStamina);
        session.EnemyCurrentHp = Math.Min(session.EnemyCurrentHp + session.EnemyHealthRegen, session.EnemyMaxHp);
        session.EnemyCurrentMana = Math.Min(session.EnemyCurrentMana + session.EnemyManaRegen + GetRowManaRegenBonus(enemyRow), session.EnemyMaxMana);
        session.EnemyCurrentStamina = Math.Min(session.EnemyCurrentStamina + session.EnemyStaminaRegen, session.EnemyMaxStamina);

        RuntimeStores.CharacterStatusEffects[session.Id] = characterEffects;
        RuntimeStores.EnemyStatusEffects[session.Id] = enemyEffects;

        await _context.SaveChangesAsync();
        return Ok(await BuildSummaryDto(session.Id));
    }

    private static void DecrementCooldowns(Dictionary<int, int> cooldowns)
    {
        var keys = cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            if (cooldowns[key] > 0)
                cooldowns[key] = Math.Max(0, cooldowns[key] - 1);
        }
    }

    private List<PassiveInteractionDto> BuildPassivePreviewInteractions(FightSession session, Character character, int manaAfterAction, int staminaAfterAction)
    {
        var activePassiveIds = session.GetCharacterActivePassiveSkills();
        var passiveInteractions = new List<PassiveInteractionDto>();

        var projectedMana = manaAfterAction;
        var projectedStamina = staminaAfterAction;

        foreach (var passiveSkillId in activePassiveIds)
        {
            var passiveSkill = character.Skills.FirstOrDefault(s => s.Id == passiveSkillId && s.Type == SkillType.Passive);
            if (passiveSkill == null)
                continue;

            var canMaintain = projectedMana >= passiveSkill.ManaCost && projectedStamina >= passiveSkill.StaminaCost;
            var reason = canMaintain
                ? null
                : $"Needs mana {passiveSkill.ManaCost} and stamina {passiveSkill.StaminaCost}";

            if (canMaintain)
            {
                projectedMana -= passiveSkill.ManaCost;
                projectedStamina -= passiveSkill.StaminaCost;
            }

            passiveInteractions.Add(new PassiveInteractionDto(
                passiveSkill.Id,
                passiveSkill.Name,
                true,
                canMaintain,
                canMaintain,
                reason,
                passiveSkill.ManaCost,
                passiveSkill.StaminaCost));
        }

        return passiveInteractions;
    }

    private ActionResult<ApiEnvelope<T>> ToEnvelopeResult<T>(ActionResult<T> actionResult)
    {
        if (actionResult.Result == null)
            return Ok(Envelope(actionResult.Value));

        if (actionResult.Result is CreatedAtActionResult created && created.Value is T createdValue)
            return StatusCode(StatusCodes.Status201Created, Envelope(createdValue));

        if (actionResult.Result is OkObjectResult ok && ok.Value is T okValue)
            return Ok(Envelope(okValue));

        if (actionResult.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? StatusCodes.Status500InternalServerError;
            var message = ExtractMessage(objectResult.Value) ?? "Request failed";
            return StatusCode(statusCode, EnvelopeError<T>(MapErrorCode(statusCode), message, objectResult.Value));
        }

        if (actionResult.Result is StatusCodeResult statusOnly)
        {
            var statusCode = statusOnly.StatusCode;
            var message = $"Request failed with status {statusCode}";
            return StatusCode(statusCode, EnvelopeError<T>(MapErrorCode(statusCode), message));
        }

        return StatusCode(StatusCodes.Status500InternalServerError,
            EnvelopeError<T>("internal_error", "Unexpected action result"));
    }

    private ApiEnvelope<T> Envelope<T>(T? data, ApiPaginationMeta? pagination = null)
    {
        var meta = new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier, pagination);
        return new ApiEnvelope<T>(data, meta);
    }

    private ApiEnvelope<T> EnvelopeError<T>(string code, string message, object? details = null)
    {
        var meta = new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier);
        return new ApiEnvelope<T>(default, meta, new ApiError(code, message, details));
    }

    private static string MapErrorCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "bad_request",
            StatusCodes.Status401Unauthorized => "unauthorized",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status409Conflict => "conflict",
            _ => "request_failed"
        };
    }

    private static string? ExtractMessage(object? value)
    {
        if (value == null)
            return null;

        if (value is string raw)
            return raw;

        var prop = value.GetType().GetProperty("message");
        var msg = prop?.GetValue(value) as string;
        return msg;
    }

    private static double GetResistanceMultiplier(ElementType element, EnemyClass? defender)
    {
        if (defender == null)
            return 1.0;

        return element switch
        {
            ElementType.Physical => defender.PhysicalResistance,
            ElementType.Fire => defender.FireResistance,
            ElementType.Ice => defender.IceResistance,
            ElementType.Lightning => defender.LightningResistance,
            ElementType.Poison => defender.PoisonResistance,
            ElementType.Holy => defender.HolyResistance,
            ElementType.Shadow => defender.ShadowResistance,
            ElementType.Arcane => defender.ArcaneResistance,
            _ => 1.0
        };
    }

    private static double GetResistanceMultiplier(ElementType element, Class? defender)
    {
        if (defender == null)
            return 1.0;

        return element switch
        {
            ElementType.Physical => defender.PhysicalResistance,
            ElementType.Fire => defender.FireResistance,
            ElementType.Ice => defender.IceResistance,
            ElementType.Lightning => defender.LightningResistance,
            ElementType.Poison => defender.PoisonResistance,
            ElementType.Holy => defender.HolyResistance,
            ElementType.Shadow => defender.ShadowResistance,
            ElementType.Arcane => defender.ArcaneResistance,
            _ => 1.0
        };
    }

    private static string DescribeElementEffect(double multiplier)
    {
        if (multiplier >= 1.15)
            return "Effective";
        if (multiplier <= 0.85)
            return "Resisted";
        return "Normal";
    }

    private static double GetRowDamageMultiplier(BattleRow attacker, BattleRow defender)
    {
        var attackerMultiplier = attacker switch
        {
            BattleRow.Front => 1.1,
            BattleRow.Mid => 1.0,
            BattleRow.Back => 0.92,
            _ => 1.0
        };

        var defenderMultiplier = defender switch
        {
            BattleRow.Front => 1.05,
            BattleRow.Mid => 1.0,
            BattleRow.Back => 0.93,
            _ => 1.0
        };

        return attackerMultiplier * defenderMultiplier;
    }

    private static int GetRowManaRegenBonus(BattleRow row)
    {
        return row switch
        {
            BattleRow.Back => 2,
            BattleRow.Mid => 1,
            _ => 0
        };
    }

    private static bool HasEffect(List<StatusEffectState> effects, StatusEffectType type)
    {
        return effects.Any(e => e.Type == type && e.RemainingTurns > 0);
    }

    private static (int Damage, bool SkipAction, List<StatusEffectState> UpdatedEffects) ApplyStatusEffects(List<StatusEffectState> effects)
    {
        var damage = 0;
        var skip = false;
        var updated = new List<StatusEffectState>();

        foreach (var effect in effects)
        {
            if (effect.RemainingTurns <= 0)
                continue;

            switch (effect.Type)
            {
                case StatusEffectType.Burn:
                    damage += 4 * effect.Potency;
                    break;
                case StatusEffectType.Poison:
                    damage += 3 * effect.Potency;
                    break;
                case StatusEffectType.Bleed:
                    damage += 5 * effect.Potency;
                    break;
                case StatusEffectType.Freeze:
                case StatusEffectType.Stun:
                    skip = true;
                    break;
            }

            if (effect.RemainingTurns > 1)
                updated.Add(effect with { RemainingTurns = effect.RemainingTurns - 1 });
        }

        return (damage, skip, updated);
    }

    private static List<StatusEffectState> MergeEffects(List<StatusEffectState> current, IEnumerable<StatusEffectState> incoming)
    {
        var merged = current.ToList();
        foreach (var add in incoming)
        {
            var index = merged.FindIndex(e => e.Type == add.Type);
            if (index >= 0)
            {
                var existing = merged[index];
                merged[index] = existing with { RemainingTurns = Math.Max(existing.RemainingTurns, add.RemainingTurns), Potency = Math.Max(existing.Potency, add.Potency) };
            }
            else
            {
                merged.Add(add);
            }
        }

        return merged;
    }

    private static IEnumerable<StatusEffectState> BuildInflictedEffects(Skill skill)
    {
        return skill.Element switch
        {
            ElementType.Fire => [new StatusEffectState(StatusEffectType.Burn, 2, 1, skill.Id)],
            ElementType.Poison => [new StatusEffectState(StatusEffectType.Poison, 3, 1, skill.Id)],
            ElementType.Ice => [new StatusEffectState(StatusEffectType.Freeze, 1, 1, skill.Id)],
            ElementType.Lightning => [new StatusEffectState(StatusEffectType.Stun, 1, 1, skill.Id)],
            ElementType.Physical => [new StatusEffectState(StatusEffectType.Bleed, 2, 1, skill.Id)],
            ElementType.Shadow => [new StatusEffectState(StatusEffectType.ShieldBreak, 2, 1, skill.Id)],
            _ => []
        };
    }

    private static string BuildIdempotencyKey(string operation, params object[] parts)
    {
        var raw = string.Join("|", parts.Select(p => p?.ToString() ?? string.Empty));
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{operation}|{raw}"));
        return Convert.ToHexString(bytes);
    }

    private static bool TryGetIdempotentResponse<T>(string key, out ActionResult<T>? response)
    {
        response = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!RuntimeStores.IdempotencyResponses.TryGetValue(key, out var cached))
            return false;

        if (cached is ActionResult<T> typed)
        {
            response = typed;
            return true;
        }

        return false;
    }

    private static void StoreIdempotentResponse<T>(string key, ActionResult<T> response)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        RuntimeStores.IdempotencyResponses[key] = response;
    }

    private async Task GrantExperience(Character character, int reward)
    {
        const double levelXpBase = 50.0;
        const double levelXpGrowth = 1.08;

        var xp = character.Experience + reward;
        double threshold = levelXpBase * Math.Pow(levelXpGrowth, character.Level);

        while (xp >= threshold)
        {
            xp -= (int)Math.Floor(threshold);
            character.Level += 1;

            // Improve base stats on level-up
            character.MaxHealth += 10;
            character.Health += 10;
            character.MaxMana += 5;
            character.Mana += 5;
            character.MaxStamina += 5;
            character.Stamina += 5;
            character.Attack += 2;
            character.Defense += 2;
            character.Speed += 1;
            character.Magic += 1;

            threshold *= levelXpGrowth;
        }

        character.Experience = xp;
        character.UpdatedAt = DateTime.UtcNow;
        _context.Characters.Update(character);
        await _context.SaveChangesAsync();
    }

    private void AddProgressionEvent(string type, string message, int? playerId = null, int? characterId = null, string? metadata = null)
    {
        _context.ProgressionEvents.Add(new ProgressionEvent
        {
            Timestamp = DateTime.UtcNow,
            Type = type,
            Message = message,
            PlayerId = playerId,
            CharacterId = characterId,
            Metadata = metadata
        });
    }

    private static FightSession CreateSessionObject(Character character, Enemy enemy, int playerId)
    {
        var session = new FightSession
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CharacterId = character.Id,
            EnemyId = enemy.Id,
            CharacterName = character.Name,
            EnemyName = enemy.Name,
            CharacterCurrentHp = character.Health,
            EnemyCurrentHp = enemy.Health,
            CharacterMaxHp = character.Health,
            EnemyMaxHp = enemy.Health,
            CharacterCurrentMana = character.Mana,
            EnemyCurrentMana = enemy.Mana,
            CharacterMaxMana = character.Mana,
            EnemyMaxMana = enemy.Mana,
            CharacterCurrentStamina = character.Stamina,
            EnemyCurrentStamina = enemy.Stamina,
            CharacterMaxStamina = character.Stamina,
            EnemyMaxStamina = enemy.Stamina,
            CharacterHealthRegen = character.HealthRegen,
            CharacterManaRegen = character.ManaRegen,
            CharacterStaminaRegen = character.StaminaRegen,
            EnemyHealthRegen = enemy.HealthRegen,
            EnemyManaRegen = enemy.ManaRegen,
            EnemyStaminaRegen = enemy.StaminaRegen,
            CharacterAttack = character.Attack,
            CharacterDefense = character.Defense,
            EnemyAttack = enemy.Attack,
            EnemyDefense = enemy.Defense,
            CharacterLevel = character.Level,
            EnemyLevel = enemy.Level,
            IsVictory = false,
            CurrentTurn = 1,
            Status = FightStatus.InProgress,
            LastActionAt = DateTime.UtcNow,
            CharacterCooldownJson = System.Text.Json.JsonSerializer.Serialize(character.Skills.ToDictionary(s => s.Id, s => 0)),
            EnemyCooldownJson = System.Text.Json.JsonSerializer.Serialize(enemy.Skills.ToDictionary(s => s.Id, s => 0)),
            CharacterSkillIdsJson = System.Text.Json.JsonSerializer.Serialize(character.Skills.Select(s => s.Id).ToList()),
            EnemySkillIdsJson = System.Text.Json.JsonSerializer.Serialize(enemy.Skills.Select(s => s.Id).ToList())
        };

        return session;
    }

    private async Task<FightSummaryDto> BuildSummaryDto(Guid id)
    {
        var session = await _context.FightSessions
            .Include(fs => fs.Moves)
            .FirstOrDefaultAsync(fs => fs.Id == id);
        if (session == null)
            throw new InvalidOperationException("Could not load fight session");

        var character = await _context.Characters
            .Include(c => c.Skills)
            .Include(c => c.Class)
            .FirstOrDefaultAsync(c => c.Id == session.CharacterId)
            ?? throw new InvalidOperationException("Could not load character for fight summary");

        var enemy = await _context.Enemies
            .Include(e => e.Skills)
            .Include(e => e.EnemyClass)
            .FirstOrDefaultAsync(e => e.Id == session.EnemyId)
            ?? throw new InvalidOperationException("Could not load enemy for fight summary");

        return new FightSummaryDto(
            session.Id,
            session.CharacterId,
            session.EnemyId,
            session.CharacterName,
            session.EnemyName,
            session.CharacterCurrentHp,
            session.CharacterMaxHp,
            session.CharacterCurrentMana,
            session.CharacterMaxMana,
            session.CharacterCurrentStamina,
            session.CharacterMaxStamina,
            session.CharacterHealthRegen,
            session.CharacterManaRegen,
            session.CharacterStaminaRegen,
            session.EnemyCurrentHp,
            session.EnemyMaxHp,
            session.EnemyCurrentMana,
            session.EnemyMaxMana,
            session.EnemyCurrentStamina,
            session.EnemyMaxStamina,
            session.EnemyHealthRegen,
            session.EnemyManaRegen,
            session.EnemyStaminaRegen,
            session.CharacterAttack,
            session.CharacterDefense,
            session.EnemyAttack,
            session.EnemyDefense,
            session.CharacterLevel,
            session.EnemyLevel,
            session.IsVictory,
            session.Status,
            session.CurrentTurn,
            session.Moves.OrderBy(m => m.Turn).Select(m => BuildMoveDto(m, character, enemy)).ToList(),
            session.CharacterCooldownJson,
            session.EnemyCooldownJson,
            session.GetCharacterActivePassiveSkills(),
            GetEnemyIntent(session, enemy),
            (RuntimeStores.CharacterRows.TryGetValue(session.Id, out var cRow) ? cRow : BattleRow.Mid).ToString(),
            (RuntimeStores.EnemyRows.TryGetValue(session.Id, out var eRow) ? eRow : BattleRow.Mid).ToString(),
            "multiplier",
            RuntimeStores.CharacterStatusEffects.TryGetValue(session.Id, out var characterEffects) ? characterEffects : new List<StatusEffectState>(),
            RuntimeStores.EnemyStatusEffects.TryGetValue(session.Id, out var enemyEffects) ? enemyEffects : new List<StatusEffectState>());
    }

    private static FightMoveDto BuildMoveDto(FightMove move, Character character, Enemy enemy)
    {
        var skill = move.IsPlayer
            ? character.Skills.FirstOrDefault(s => s.Id == move.SkillId)
            : enemy.Skills.FirstOrDefault(s => s.Id == move.SkillId);

        var element = skill?.Element.ToString() ?? ElementType.Physical.ToString();
        var resisted = 1.0;
        if (skill != null)
        {
            resisted = move.IsPlayer
                ? GetResistanceMultiplier(skill.Element, enemy.EnemyClass)
                : GetResistanceMultiplier(skill.Element, character.Class);
        }

        return new FightMoveDto(
            move.Turn,
            move.IsPlayer,
            move.SkillId,
            move.Damage,
            move.Description,
            move.Timestamp,
            element,
            false,
            false,
            false,
            resisted,
            null);
    }

    private static EnemyIntentDto GetEnemyIntent(FightSession session, Enemy enemy)
    {
        var enemyCooldowns = session.GetEnemyCooldowns();
        var nextSkill = enemy.Skills
            .Where(s => enemyCooldowns.TryGetValue(s.Id, out var cd) && cd == 0)
            .OrderBy(s => s.Cooldown)
            .ThenBy(s => s.Id)
            .FirstOrDefault();

        if (nextSkill == null)
            return new EnemyIntentDto("attack", null, "Enemy is likely to use a basic attack.");

        var intent = nextSkill.Type switch
        {
            SkillType.Passive => "buff",
            SkillType.Ultimate => "attack",
            _ => "attack"
        };

        if (nextSkill.Name.Contains("heal", StringComparison.OrdinalIgnoreCase))
            intent = "heal";
        if (nextSkill.Name.Contains("curse", StringComparison.OrdinalIgnoreCase) || nextSkill.Name.Contains("debuff", StringComparison.OrdinalIgnoreCase))
            intent = "debuff";

        return new EnemyIntentDto(intent, nextSkill.Name, $"Enemy may use {nextSkill.Name}.");
    }
}

public record StartFightDto(int PlayerId, int CharacterId, int? EnemyId = null);
public record CreateMatchDto(int PlayerId, int CharacterId, int EnemyId);
public record LockFightDto(int PlayerId);
public record FightTurnDto(int PlayerId, int SkillId);
public record FightTurnV1Dto(int PlayerId, int SkillId, int? ExpectedTurn = null);

public record FightMoveDto(
    int Turn,
    bool IsPlayer,
    int SkillId,
    int Damage,
    string Description,
    DateTime Timestamp,
    string Element,
    bool Crit,
    bool Dodged,
    bool Blocked,
    double ResistedPercent,
    int? SourcePassiveId);

public record FightSimulationRequest(int Seed, int PlayerId, int CharacterId, int EnemyId, int MaxTurns = 40);

public record FightSimulationTurnDto(
    int Turn,
    string PlayerAction,
    int PlayerDamage,
    string EnemyAction,
    int EnemyDamage,
    int CharacterHpAfter,
    int EnemyHpAfter);

public record FightSimulationAggregateDto(
    int TotalPlayerDamage,
    int TotalEnemyDamage,
    double AveragePlayerDamage,
    double AverageEnemyDamage,
    int CharacterManaRemaining,
    int CharacterStaminaRemaining);

public record FightSimulationResultDto(
    int Seed,
    int CharacterId,
    int EnemyId,
    bool IsVictory,
    int Turns,
    List<FightSimulationTurnDto> TurnLog,
    FightSimulationAggregateDto Aggregate);

public record SetBattlePositionDto(BattleRow CharacterRow, BattleRow EnemyRow);

public record EnemyIntentDto(string Intent, string? SkillName, string Description);

public record FightActionPreviewRequest(int PlayerId, int SkillId);

public record PassiveInteractionDto(
    int SkillId,
    string Name,
    bool Active,
    bool WillRemainActive,
    bool CanMaintain,
    string? Reason,
    int ManaCost,
    int StaminaCost);

public record FightActionPreviewDto(
    bool CanUse,
    int SkillId,
    string SkillName,
    int RemainingCooldown,
    bool HasEnoughMana,
    bool HasEnoughStamina,
    int ManaAfterAction,
    int StaminaAfterAction,
    List<PassiveInteractionDto> PassiveInteractions,
    List<string> Reasons);

public record FightSummaryDto(
    Guid Id,
    int CharacterId,
    int EnemyId,
    string CharacterName,
    string EnemyName,
    int CharacterCurrentHp,
    int CharacterMaxHp,
    int CharacterCurrentMana,
    int CharacterMaxMana,
    int CharacterCurrentStamina,
    int CharacterMaxStamina,
    int CharacterHealthRegen,
    int CharacterManaRegen,
    int CharacterStaminaRegen,
    int EnemyCurrentHp,
    int EnemyMaxHp,
    int EnemyCurrentMana,
    int EnemyMaxMana,
    int EnemyCurrentStamina,
    int EnemyMaxStamina,
    int EnemyHealthRegen,
    int EnemyManaRegen,
    int EnemyStaminaRegen,
    int CharacterAttack,
    int CharacterDefense,
    int EnemyAttack,
    int EnemyDefense,
    int CharacterLevel,
    int EnemyLevel,
    bool IsVictory,
    FightStatus Status,
    int CurrentTurn,
    List<FightMoveDto> Moves,
    string CharacterCooldownJson,
    string EnemyCooldownJson,
    List<int> CharacterActivePassiveSkills,
    EnemyIntentDto EnemyIntent,
    string CharacterRow,
    string EnemyRow,
    string ResistanceFormat,
    List<StatusEffectState> CharacterStatusEffects,
    List<StatusEffectState> EnemyStatusEffects);

public record AvailableFightDto(Guid Id, string CharacterName, string EnemyName, int CurrentTurn);

public record ActivatePassiveSkillDto(int SkillId);

public record DeactivatePassiveSkillDto(int SkillId);