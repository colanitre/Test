using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RpgApi.Data;
using RpgApi.Models;

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

        var playerSkill = character.Skills.FirstOrDefault(s => s.Id == dto.SkillId);
        if (playerSkill == null)
            return BadRequest(new { message = "Invalid skill" });

        if (character.Level < playerSkill.RequiredLevel)
            return BadRequest(new { message = $"Character must be level {playerSkill.RequiredLevel} or higher to use {playerSkill.Name}" });

        if (!characterCooldowns.TryGetValue(playerSkill.Id, out var cd) || cd > 0)
            return BadRequest(new { message = "Skill is on cooldown" });

        var charPower = character.Attack * _damageConfig.CharacterAttackWeight
                        + character.Magic * _damageConfig.CharacterMagicWeight
                        + character.Speed * _damageConfig.CharacterSpeedWeight;
        var skillPower = playerSkill.AttackPower * _damageConfig.SkillAttackWeight
                       + playerSkill.MagicPower * _damageConfig.SkillMagicWeight
                       + playerSkill.SpeedModifier * _damageConfig.SkillSpeedWeight;
        var charDefense = enemy.Defense * _damageConfig.EnemyDefenseWeight
                        + enemy.Magic * _damageConfig.EnemyMagicDefenseWeight
                        + enemy.Stamina * _damageConfig.EnemyStaminaDefenseWeight;

        var playerElementMultiplier = GetResistanceMultiplier(playerSkill.Element, enemy.EnemyClass);
        var playerEffectiveSkillPower = skillPower * playerSkill.ElementPowerMultiplier;
        var playerEffectiveness = DescribeElementEffect(playerElementMultiplier);

        var basePlDamage = Math.Max(_damageConfig.MinimumDamage,
            (int)Math.Floor((charPower + playerEffectiveSkillPower - charDefense) * playerElementMultiplier));
        var plDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(basePlDamage * (0.9 + random.NextDouble() * 0.2)));
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

        characterCooldowns[playerSkill.Id] = playerSkill.Cooldown;

        if (session.EnemyCurrentHp <= 0)
        {
            session.EnemyCurrentHp = 0;
            session.Status = FightStatus.Completed;
            session.IsVictory = true;
            session.LastActionAt = DateTime.UtcNow;
            await GrantExperience(character, enemy.ExperienceReward);
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

        if (enemySkill != null)
        {
            var enemySkillPower = enemySkill.AttackPower * _damageConfig.SkillAttackWeight
                                + enemySkill.MagicPower * _damageConfig.SkillMagicWeight
                                + enemySkill.SpeedModifier * _damageConfig.SkillSpeedWeight;
            var enemyElementMultiplier = GetResistanceMultiplier(enemySkill.Element, character.Class);
            var enemyEffectiveSkillPower = enemySkillPower * enemySkill.ElementPowerMultiplier;
            var enemyEffectiveness = DescribeElementEffect(enemyElementMultiplier);
            enemyCooldowns[enemySkill.Id] = enemySkill.Cooldown;
            var baseEnemyDamage = Math.Max(_damageConfig.MinimumDamage,
                (int)Math.Floor((enemyPower + enemyEffectiveSkillPower - enemyDefense * 0.5) * enemyElementMultiplier));
            enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(baseEnemyDamage * (0.9 + random.NextDouble() * 0.2)));
            enemyEvent = $"Enemy uses {enemySkill.Name} [{enemySkill.Element}] ({enemyEffectiveness}); deals {enemyDamage} damage.";
        }
        else
        {
            var baseEnemyDamage = Math.Max(_damageConfig.MinimumDamage,
                (int)Math.Floor(enemyPower - enemyDefense * 0.5));
            enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(baseEnemyDamage * (0.9 + random.NextDouble() * 0.2)));
            enemyEvent = $"Enemy attacks normally; deals {enemyDamage} damage.";
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
        session.CharacterCurrentMana = Math.Min(session.CharacterCurrentMana + session.CharacterManaRegen, session.CharacterMaxMana);
        session.CharacterCurrentStamina = Math.Min(session.CharacterCurrentStamina + session.CharacterStaminaRegen, session.CharacterMaxStamina);
        session.EnemyCurrentHp = Math.Min(session.EnemyCurrentHp + session.EnemyHealthRegen, session.EnemyMaxHp);
        session.EnemyCurrentMana = Math.Min(session.EnemyCurrentMana + session.EnemyManaRegen, session.EnemyMaxMana);
        session.EnemyCurrentStamina = Math.Min(session.EnemyCurrentStamina + session.EnemyStaminaRegen, session.EnemyMaxStamina);

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

    private async Task GrantExperience(Character character, int reward)
    {
        var xp = character.Experience + reward;
        double threshold = 100.0 * Math.Pow(1.2, character.Level);

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

            threshold *= 1.2;
        }

        character.Experience = xp;
        character.UpdatedAt = DateTime.UtcNow;
        _context.Characters.Update(character);
        await _context.SaveChangesAsync();
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
            session.Moves.OrderBy(m => m.Turn).Select(m => new FightMoveDto(m.Turn, m.IsPlayer, m.SkillId, m.Damage, m.Description, m.Timestamp)).ToList(),
            session.CharacterCooldownJson,
            session.EnemyCooldownJson,
            session.GetCharacterActivePassiveSkills());
    }
}

public record StartFightDto(int PlayerId, int CharacterId, int? EnemyId = null);
public record CreateMatchDto(int PlayerId, int CharacterId, int EnemyId);
public record LockFightDto(int PlayerId);
public record FightTurnDto(int PlayerId, int SkillId);

public record FightMoveDto(int Turn, bool IsPlayer, int SkillId, int Damage, string Description, DateTime Timestamp);

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
    List<int> CharacterActivePassiveSkills);

public record AvailableFightDto(Guid Id, string CharacterName, string EnemyName, int CurrentTurn);

public record ActivatePassiveSkillDto(int SkillId);

public record DeactivatePassiveSkillDto(int SkillId);