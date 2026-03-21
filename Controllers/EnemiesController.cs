using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers
{

[ApiController]
[Route("api/[controller]")]
public class EnemiesController : ControllerBase
{
    private readonly RpgContext _context;
    private readonly ILogger<EnemiesController> _logger;

    public EnemiesController(RpgContext context, ILogger<EnemiesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all enemies
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnemyDetailDto>>> GetEnemies()
    {
        _logger.LogInformation("Fetching all enemies");

        var enemies = await _context.Enemies
            .Include(e => e.EnemyClass)
            .Include(e => e.Skills)
            .ToListAsync();

        return Ok(enemies.Select(ToDto));
    }

    /// <summary>
    /// Get a specific enemy
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EnemyDetailDto>> GetEnemy(int id)
    {
        _logger.LogInformation("Fetching enemy ID: {EnemyId}", id);

        var enemy = await _context.Enemies
            .Include(e => e.EnemyClass)
            .Include(e => e.Skills)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enemy == null)
        {
            _logger.LogWarning("Enemy with ID {EnemyId} not found", id);
            return NotFound(new { message = "Enemy not found" });
        }

        return Ok(ToDto(enemy));
    }

    /// <summary>
    /// Get enemies by type
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<ActionResult<IEnumerable<EnemyDetailDto>>> GetEnemiesByType(EnemyType type)
    {
        _logger.LogInformation("Fetching enemies of type: {EnemyType}", type);

        var enemies = await _context.Enemies
            .Include(e => e.EnemyClass)
            .Include(e => e.Skills)
            .Where(e => e.Type == type)
            .ToListAsync();

        return Ok(enemies.Select(ToDto));
    }

    /// <summary>
    /// Get enemies by level range
    /// </summary>
    [HttpGet("level/{minLevel}/{maxLevel}")]
    public async Task<ActionResult<IEnumerable<EnemyDetailDto>>> GetEnemiesByLevel(int minLevel, int maxLevel)
    {
        _logger.LogInformation("Fetching enemies between levels {MinLevel} and {MaxLevel}", minLevel, maxLevel);

        var enemies = await _context.Enemies
            .Include(e => e.EnemyClass)
            .Include(e => e.Skills)
            .Where(e => e.Level >= minLevel && e.Level <= maxLevel)
            .ToListAsync();

        return Ok(enemies.Select(ToDto));
    }

    /// <summary>
    /// Create a new enemy
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EnemyDetailDto>> CreateEnemy([FromBody] CreateEnemyDto createEnemyDto)
    {
        _logger.LogInformation("Creating enemy: {EnemyName}", createEnemyDto.Name);

        // Get the enemy class
        var enemyClass = await _context.EnemyClasses
            .Include(ec => ec.Skills)
            .FirstOrDefaultAsync(ec => ec.Name == createEnemyDto.EnemyClass);
        if (enemyClass == null)
        {
            _logger.LogWarning("Enemy class '{EnemyClassName}' not found", createEnemyDto.EnemyClass);
            return BadRequest(new { message = $"Enemy class '{createEnemyDto.EnemyClass}' not found" });
        }

        // Create enemy with stats initialized from enemy class
        var enemy = new Enemy(createEnemyDto.Name, enemyClass)
        {
            Description = createEnemyDto.Description
        };

        _context.Enemies.Add(enemy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Enemy created successfully with ID: {EnemyId}", enemy.Id);
        return CreatedAtAction(nameof(GetEnemy), new { id = enemy.Id }, ToDto(enemy));
    }

    /// <summary>
    /// Update an enemy
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEnemy(int id, [FromBody] UpdateEnemyDto updateEnemyDto)
    {
        _logger.LogInformation("Updating enemy ID: {EnemyId}", id);

        var enemy = await _context.Enemies
            .Include(e => e.EnemyClass)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enemy == null)
        {
            _logger.LogWarning("Enemy with ID {EnemyId} not found", id);
            return NotFound(new { message = "Enemy not found" });
        }

        // If enemy class changed, look up the new class
        if (enemy.EnemyClass?.Name != updateEnemyDto.EnemyClass)
        {
            var newClass = await _context.EnemyClasses.FirstOrDefaultAsync(ec => ec.Name == updateEnemyDto.EnemyClass);
            if (newClass == null)
            {
                _logger.LogWarning("Enemy class '{EnemyClassName}' not found", updateEnemyDto.EnemyClass);
                return BadRequest(new { message = $"Enemy class '{updateEnemyDto.EnemyClass}' not found" });
            }
            enemy.EnemyClass = newClass;
            enemy.EnemyClassId = newClass.Id;
        }

        enemy.Name = updateEnemyDto.Name;
        enemy.Description = updateEnemyDto.Description;
        enemy.Level = updateEnemyDto.Level;

        _context.Enemies.Update(enemy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Enemy with ID {EnemyId} updated successfully", id);
        return NoContent();
    }

    /// <summary>
    /// Delete an enemy
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEnemy(int id)
    {
        _logger.LogInformation("Deleting enemy ID: {EnemyId}", id);

        var enemy = await _context.Enemies.FindAsync(id);

        if (enemy == null)
        {
            _logger.LogWarning("Enemy with ID {EnemyId} not found", id);
            return NotFound(new { message = "Enemy not found" });
        }

        _context.Enemies.Remove(enemy);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Enemy with ID {EnemyId} deleted successfully", id);
        return NoContent();
    }

    private static EnemyDetailDto ToDto(Enemy enemy) =>
        new(
            enemy.Id,
            enemy.Name,
            enemy.EnemyClass != null ? new EnemyClassDto(
                enemy.EnemyClass.Id,
                enemy.EnemyClass.Name,
                enemy.EnemyClass.Description,
                enemy.EnemyClass.Type,
                enemy.EnemyClass.BaseLevel,
                enemy.EnemyClass.BaseStrength,
                enemy.EnemyClass.BaseAgility,
                enemy.EnemyClass.BaseIntelligence,
                enemy.EnemyClass.BaseWisdom,
                enemy.EnemyClass.BaseCharisma,
                enemy.EnemyClass.BaseEndurance,
                enemy.EnemyClass.BaseLuck) : null,
            enemy.Level,
            enemy.Health,
            enemy.MaxHealth,
            enemy.Mana,
            enemy.MaxMana,
            enemy.Stamina,
            enemy.MaxStamina,
            enemy.HealthRegen,
            enemy.ManaRegen,
            enemy.StaminaRegen,
            enemy.Attack,
            enemy.Defense,
            enemy.Speed,
            enemy.Magic,
            enemy.ExperienceReward,
            enemy.Description,
            enemy.Type,
            enemy.CreatedAt,
            enemy.Skills.Select(s => new SkillDto(
                s.Id,
                s.Name,
                s.Description,
                s.Type,
                s.Level,
                s.ManaCost,
                s.StaminaCost,
                s.Cooldown,
                s.RequiredLevel,
                s.AttackPower,
                s.DefensePower,
                s.SpeedModifier,
                s.MagicPower)).ToList());
}

public record EnemyDetailDto(
    int Id,
    string Name,
    EnemyClassDto? EnemyClass,
    int Level,
    int Health,
    int MaxHealth,
    int Mana,
    int MaxMana,
    int Stamina,
    int MaxStamina,
    int HealthRegen,
    int ManaRegen,
    int StaminaRegen,
    int Attack,
    int Defense,
    int Speed,
    int Magic,
    int ExperienceReward,
    string? Description,
    EnemyType Type,
    DateTime CreatedAt,
    List<SkillDto> Skills);

public record EnemyClassDto(
    int Id,
    string Name,
    string Description,
    EnemyType Type,
    int BaseLevel,
    int BaseStrength,
    int BaseAgility,
    int BaseIntelligence,
    int BaseWisdom,
    int BaseCharisma,
    int BaseEndurance,
    int BaseLuck);

public record CreateEnemyDto(
    string Name,
    string EnemyClass,
    string? Description = null);

public record UpdateEnemyDto(
    string Name,
    string EnemyClass,
    int Level,
    string? Description);
}