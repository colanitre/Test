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
            .FirstOrDefaultAsync(c => c.Id == session.CharacterId);
        if (character == null)
            return NotFound(new { message = "Character not found" });

        var enemy = await _context.Enemies
            .Include(e => e.Skills)
            .FirstOrDefaultAsync(e => e.Id == session.EnemyId);
        if (enemy == null)
            return NotFound(new { message = "Enemy not found" });

        var characterCooldowns = session.GetCharacterCooldowns();
        var enemyCooldowns = session.GetEnemyCooldowns();

        var random = new Random();

        var playerSkill = character.Skills.FirstOrDefault(s => s.Id == dto.SkillId);
        if (playerSkill == null)
            return BadRequest(new { message = "Invalid skill" });

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

        var basePlDamage = Math.Max(_damageConfig.MinimumDamage, (int)Math.Floor(charPower + skillPower - charDefense));
        var plDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(basePlDamage * (0.9 + random.NextDouble() * 0.2)));
        session.EnemyCurrentHp -= plDamage;
        session.Moves.Add(new FightMove
        {
            FightSessionId = session.Id,
            Turn = session.CurrentTurn,
            IsPlayer = true,
            SkillId = playerSkill.Id,
            Damage = plDamage,
            Description = $"Character uses {playerSkill.Name}; deals {plDamage} damage."
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
            enemyCooldowns[enemySkill.Id] = enemySkill.Cooldown;
            var baseEnemyDamage = Math.Max(_damageConfig.MinimumDamage,
                (int)Math.Floor(enemyPower + enemySkillPower - enemyDefense * 0.5));
            enemyDamage = (int)Math.Max(_damageConfig.MinimumDamage, Math.Floor(baseEnemyDamage * (0.9 + random.NextDouble() * 0.2)));
            enemyEvent = $"Enemy uses {enemySkill.Name}; deals {enemyDamage} damage.";
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

    private async Task GrantExperience(Character character, int reward)
    {
        var xp = character.Experience + reward;
        double threshold = 100.0 * Math.Pow(1.2, character.Level);

        while (xp >= threshold)
        {
            xp -= (int)Math.Floor(threshold);
            character.Level += 1;

            // Improve base stats on level-up
            character.Health += 10;
            character.Mana += 5;
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
            session.EnemyCurrentHp,
            session.EnemyMaxHp,
            session.CharacterAttack,
            session.CharacterDefense,
            session.EnemyAttack,
            session.EnemyDefense,
            session.IsVictory,
            session.Status,
            session.CurrentTurn,
            session.Moves.OrderBy(m => m.Turn).Select(m => new FightMoveDto(m.Turn, m.IsPlayer, m.SkillId, m.Damage, m.Description, m.Timestamp)).ToList(),
            session.CharacterCooldownJson,
            session.EnemyCooldownJson);
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
    int EnemyCurrentHp,
    int EnemyMaxHp,
    int CharacterAttack,
    int CharacterDefense,
    int EnemyAttack,
    int EnemyDefense,
    bool IsVictory,
    FightStatus Status,
    int CurrentTurn,
    List<FightMoveDto> Moves,
    string CharacterCooldownJson,
    string EnemyCooldownJson);

public record AvailableFightDto(Guid Id, string CharacterName, string EnemyName, int CurrentTurn);