using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers
{

[ApiController]
[Route("api/players/{playerId}/[controller]")]
public class CharactersController : ControllerBase
{
    private readonly RpgContext _context;
    private readonly ILogger<CharactersController> _logger;

    public CharactersController(RpgContext context, ILogger<CharactersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/v1/players/{playerId}/characters")]
    public async Task<ActionResult<ApiEnvelope<IEnumerable<CharacterDetailDto>>>> GetCharactersV1(int playerId)
    {
        var result = await GetCharacters(playerId);
        if (result.Result is ObjectResult objectResult)
        {
            var status = objectResult.StatusCode ?? StatusCodes.Status500InternalServerError;
            var message = objectResult.Value?.GetType().GetProperty("message")?.GetValue(objectResult.Value)?.ToString() ?? "Request failed";
            return StatusCode(status, new ApiEnvelope<IEnumerable<CharacterDetailDto>>(
                default,
                new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier),
                new ApiError("request_failed", message, objectResult.Value)));
        }

        return Ok(new ApiEnvelope<IEnumerable<CharacterDetailDto>>(
            result.Value,
            new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpGet("/api/v1/players/{playerId}/characters/{id}")]
    public async Task<ActionResult<ApiEnvelope<CharacterDetailDto>>> GetCharacterV1(int playerId, int id)
    {
        var result = await GetCharacter(playerId, id);
        if (result.Result is ObjectResult objectResult)
        {
            var status = objectResult.StatusCode ?? StatusCodes.Status500InternalServerError;
            var message = objectResult.Value?.GetType().GetProperty("message")?.GetValue(objectResult.Value)?.ToString() ?? "Request failed";
            return StatusCode(status, new ApiEnvelope<CharacterDetailDto>(
                default,
                new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier),
                new ApiError("request_failed", message, objectResult.Value)));
        }

        return Ok(new ApiEnvelope<CharacterDetailDto>(
            result.Value,
            new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpGet("/api/v1/players/{playerId}/characters/{id}/loadouts")]
    public async Task<ActionResult<ApiEnvelope<IEnumerable<CharacterLoadoutEntry>>>> GetLoadoutsV1(int playerId, int id)
    {
        var characterExists = await _context.Characters.AnyAsync(c => c.Id == id && c.PlayerId == playerId);
        if (!characterExists)
        {
            return NotFound(new ApiEnvelope<IEnumerable<CharacterLoadoutEntry>>(
                default,
                new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier),
                new ApiError("character_not_found", "Character not found")));
        }

        var entries = await _context.CharacterLoadouts
            .Where(l => l.PlayerId == playerId && l.CharacterId == id)
            .OrderByDescending(l => l.UpdatedAt)
            .Select(l => new CharacterLoadoutEntry(l.Id, l.Name, l.GetActiveSkillOrder(), l.GetPassiveSkillIds(), l.UpdatedAt))
            .ToListAsync();

        return Ok(new ApiEnvelope<IEnumerable<CharacterLoadoutEntry>>(
            entries,
            new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpPut("/api/v1/players/{playerId}/characters/{id}/loadouts/{loadoutId}")]
    public async Task<ActionResult<ApiEnvelope<CharacterLoadoutEntry>>> PutLoadoutV1(
        int playerId,
        int id,
        Guid loadoutId,
        [FromBody] UpsertLoadoutDto dto)
    {
        var character = await _context.Characters
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == playerId);
        if (character == null)
        {
            return NotFound(new ApiEnvelope<CharacterLoadoutEntry>(
                default,
                new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier),
                new ApiError("character_not_found", "Character not found")));
        }

        var skillIds = character.Skills.Select(s => s.Id).ToHashSet();
        if (dto.ActiveSkillOrder.Any(sid => !skillIds.Contains(sid)) || dto.PassiveSkillIds.Any(sid => !skillIds.Contains(sid)))
        {
            return BadRequest(new ApiEnvelope<CharacterLoadoutEntry>(
                default,
                new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier),
                new ApiError("invalid_skills", "Loadout contains skills not available to this character")));
        }

        var entry = new CharacterLoadoutEntry(loadoutId, dto.Name, dto.ActiveSkillOrder, dto.PassiveSkillIds, DateTime.UtcNow);

        var existing = await _context.CharacterLoadouts
            .FirstOrDefaultAsync(l => l.Id == loadoutId && l.PlayerId == playerId && l.CharacterId == id);
        if (existing != null)
        {
            existing.Name = entry.Name;
            existing.SetActiveSkillOrder(entry.ActiveSkillOrder);
            existing.SetPassiveSkillIds(entry.PassiveSkillIds);
            existing.UpdatedAt = entry.UpdatedAt;
        }
        else
        {
            var entity = new CharacterLoadout
            {
                Id = entry.Id,
                PlayerId = playerId,
                CharacterId = id,
                Name = entry.Name,
                UpdatedAt = entry.UpdatedAt
            };
            entity.SetActiveSkillOrder(entry.ActiveSkillOrder);
            entity.SetPassiveSkillIds(entry.PassiveSkillIds);
            await _context.CharacterLoadouts.AddAsync(entity);
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiEnvelope<CharacterLoadoutEntry>(
            entry,
            new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    /// <summary>
    /// Get all characters for a specific player
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CharacterDetailDto>>> GetCharacters(int playerId)
    {
        _logger.LogInformation("Fetching characters for player ID: {PlayerId}", playerId);
        
        // Verify player exists
        var playerExists = await _context.Players.AnyAsync(p => p.Id == playerId);
        if (!playerExists)
        {
            _logger.LogWarning("Player with ID {PlayerId} not found", playerId);
            return NotFound(new { message = "Player not found" });
        }

        var characters = await _context.Characters
            .Where(c => c.PlayerId == playerId)
            .Include(c => c.Class)
            .Include(c => c.Skills)
            .ToListAsync();

        return Ok(characters.Select(ToDto));
    }

    /// <summary>
    /// Get a specific character
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CharacterDetailDto>> GetCharacter(int playerId, int id)
    {
        _logger.LogInformation("Fetching character ID: {CharacterId} for player ID: {PlayerId}", id, playerId);
        
        var character = await _context.Characters
            .Include(c => c.Class)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == playerId);

        if (character == null)
        {
            _logger.LogWarning("Character with ID {CharacterId} for player {PlayerId} not found", id, playerId);
            return NotFound(new { message = "Character not found" });
        }

        return Ok(ToDto(character));
    }

    /// <summary>
    /// Get available class upgrades for a character.
    /// </summary>
    [HttpGet("{id}/class-upgrades")]
    public async Task<ActionResult<IEnumerable<ClassUpgradeOptionDto>>> GetClassUpgrades(int playerId, int id)
    {
        var character = await _context.Characters
            .Include(c => c.Class)
            .FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == playerId);

        if (character == null)
            return NotFound(new { message = "Character not found" });

        if (character.Class == null)
            return BadRequest(new { message = "Character has no class" });

        var archetype = character.Class.Archetype;
        var nextTier = character.Class.Tier + 1;

        var options = await _context.Classes
            .Include(c => c.Skills)
            .Where(c => c.Archetype == archetype && c.Tier == nextTier && c.RequiredLevel <= character.Level)
            .Where(c => !character.AdvancedPath.HasValue || c.Branch == character.AdvancedPath.Value)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(options.Select(c => new ClassUpgradeOptionDto(
            c.Id,
            c.Name,
            c.Archetype,
            c.Tier,
            c.RequiredLevel,
            c.IsAdvanced,
            c.Branch,
            c.BaseStrength,
            c.BaseAgility,
            c.BaseIntelligence,
            c.BaseWisdom,
            c.BaseCharisma,
            c.BaseEndurance,
            c.BaseLuck,
            c.Skills
                .OrderBy(s => s.RequiredLevel)
                .ThenBy(s => s.Name)
                .Select(s => new ClassUpgradeSkillDto(
                    s.Id,
                    s.Name,
                    s.Type,
                    s.RequiredLevel,
                    s.ManaCost,
                    s.StaminaCost,
                    s.Cooldown,
                    s.AttackPower,
                    s.DefensePower,
                    s.SpeedModifier,
                    s.MagicPower,
                    s.Element,
                    s.ElementPowerMultiplier))
                .ToList())));
    }

    /// <summary>
    /// Upgrade a character to an advanced class for their archetype.
    /// </summary>
    [HttpPost("{id}/class-upgrade")]
    public async Task<ActionResult<CharacterDetailDto>> UpgradeClass(int playerId, int id, [FromBody] UpgradeClassDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.ClassName))
            return BadRequest(new { message = "ClassName is required" });

        var character = await _context.Characters
            .Include(c => c.Class)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == playerId);

        if (character == null)
            return NotFound(new { message = "Character not found" });

        if (character.Class == null)
            return BadRequest(new { message = "Character has no class" });

        var targetClass = await _context.Classes
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Name == dto.ClassName);

        if (targetClass == null)
            return NotFound(new { message = "Target class not found" });

        if (targetClass.Archetype != character.Class.Archetype)
            return BadRequest(new { message = "Can only upgrade within the same archetype" });

        if (targetClass.Tier != character.Class.Tier + 1)
            return BadRequest(new { message = "Can only upgrade to the next tier" });

        if (character.Level < targetClass.RequiredLevel)
            return BadRequest(new { message = $"Character must be level {targetClass.RequiredLevel} to upgrade" });

        if (character.AdvancedPath.HasValue && targetClass.Branch != character.AdvancedPath.Value)
            return BadRequest(new { message = "Upgrade path is locked. You must continue with your chosen branch." });

        if (!character.AdvancedPath.HasValue && targetClass.Tier > 0)
            character.AdvancedPath = targetClass.Branch;

        character.ClassId = targetClass.Id;
        character.Class = targetClass;
        character.Skills = targetClass.Skills.ToList();
        character.RecalculateDerivedStats();
        character.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToDto(character));
    }

    /// <summary>
    /// Create a new character for a player
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CharacterDetailDto>> CreateCharacter(int playerId, [FromBody] CreateCharacterDto createCharacterDto)
    {
        _logger.LogInformation("Creating character for player ID: {PlayerId}", playerId);
        
        // Verify player exists
        var player = await _context.Players.FindAsync(playerId);
        if (player == null)
        {
            _logger.LogWarning("Player with ID {PlayerId} not found", playerId);
            return NotFound(new { message = "Player not found" });
        }

        // Get the character class with skills
        var characterClass = await _context.Classes
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Name == createCharacterDto.Class);
        if (characterClass == null)
        {
            _logger.LogWarning("Character class '{ClassName}' not found", createCharacterDto.Class);
            return BadRequest(new { message = $"Character class '{createCharacterDto.Class}' not found" });
        }

        // Create character with stats initialized from class
        var character = new Character(createCharacterDto.Name, characterClass, playerId)
        {
            Description = createCharacterDto.Description,
            Skills = characterClass.Skills.ToList() // Assign skills from the class
        };

        _context.Characters.Add(character);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Character created successfully with ID: {CharacterId}", character.Id);
        return CreatedAtAction(nameof(GetCharacter), new { playerId, id = character.Id }, ToDto(character));
    }

    /// <summary>
    /// Update a character
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCharacter(int playerId, int id, [FromBody] UpdateCharacterDto updateCharacterDto)
    {
        _logger.LogInformation("Updating character ID: {CharacterId} for player ID: {PlayerId}", id, playerId);
        
        var character = await _context.Characters
            .Include(c => c.Class)
            .FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == playerId);

        if (character == null)
        {
            _logger.LogWarning("Character with ID {CharacterId} for player {PlayerId} not found", id, playerId);
            return NotFound(new { message = "Character not found" });
        }

        // If class name changed, look up the new class
        if (character.Class?.Name != updateCharacterDto.Class)
        {
            var newClass = await _context.Classes.FirstOrDefaultAsync(c => c.Name == updateCharacterDto.Class);
            if (newClass == null)
            {
                _logger.LogWarning("Character class '{ClassName}' not found", updateCharacterDto.Class);
                return BadRequest(new { message = $"Character class '{updateCharacterDto.Class}' not found" });
            }
            character.Class = newClass;
            character.ClassId = newClass.Id;
        }

        character.Name = updateCharacterDto.Name;
        character.Description = updateCharacterDto.Description;
        character.UpdatedAt = DateTime.UtcNow;

        _context.Characters.Update(character);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Character with ID {CharacterId} updated successfully", id);
        return NoContent();
    }

    /// <summary>
    /// Delete a character
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCharacter(int playerId, int id)
    {
        _logger.LogInformation("Deleting character ID: {CharacterId} for player ID: {PlayerId}", id, playerId);
        
        var character = await _context.Characters
            .FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == playerId);

        if (character == null)
        {
            _logger.LogWarning("Character with ID {CharacterId} for player {PlayerId} not found", id, playerId);
            return NotFound(new { message = "Character not found" });
        }

        _context.Characters.Remove(character);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Character with ID {CharacterId} deleted successfully", id);
        return NoContent();
    }

    private static CharacterDetailDto ToDto(Character character) =>
        new(
            character.Id,
            character.Name,
            character.Class != null ? new ClassDto(
                character.Class.Id,
                character.Class.Name,
                character.Class.Archetype,
                character.Class.Tier,
                character.Class.RequiredLevel,
                character.Class.IsAdvanced,
                character.Class.Branch,
                character.Class.BaseStrength,
                character.Class.BaseAgility,
                character.Class.BaseIntelligence,
                character.Class.BaseWisdom,
                character.Class.BaseCharisma,
                character.Class.BaseEndurance,
                character.Class.BaseLuck,
                character.Class.PhysicalResistance,
                character.Class.FireResistance,
                character.Class.IceResistance,
                character.Class.LightningResistance,
                character.Class.PoisonResistance,
                character.Class.HolyResistance,
                character.Class.ShadowResistance,
                character.Class.ArcaneResistance) : null,
            character.Level,
            character.Health,
            character.MaxHealth,
            character.Mana,
            character.MaxMana,
            character.Stamina,
            character.MaxStamina,
            character.HealthRegen,
            character.ManaRegen,
            character.StaminaRegen,
            character.Experience,
            character.Description,
            character.CreatedAt,
            character.UpdatedAt,
            character.PlayerId,
            character.AdvancedPath,
            character.Skills.Select(s => new SkillDto(
                s.Id,
                s.Name,
                s.Description,
                s.Type,
                s.Element,
                s.ElementPowerMultiplier,
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

public record CharacterDetailDto(
    int Id,
    string Name,
    ClassDto? Class,
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
    int Experience,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int PlayerId,
    int? AdvancedPath,
    List<SkillDto> Skills);

public record ClassDto(
    int Id,
    string Name,
    string Archetype,
    int Tier,
    int RequiredLevel,
    bool IsAdvanced,
    int Branch,
    int BaseStrength,
    int BaseAgility,
    int BaseIntelligence,
    int BaseWisdom,
    int BaseCharisma,
    int BaseEndurance,
    int BaseLuck,
    double PhysicalResistance,
    double FireResistance,
    double IceResistance,
    double LightningResistance,
    double PoisonResistance,
    double HolyResistance,
    double ShadowResistance,
    double ArcaneResistance);

public record SkillDto(
    int Id,
    string Name,
    string Description,
    SkillType Type,
    ElementType Element,
    double ElementPowerMultiplier,
    int Level,
    int ManaCost,
    int StaminaCost,
    int Cooldown,
    int RequiredLevel,
    int AttackPower,
    int DefensePower,
    int SpeedModifier,
    int MagicPower);

public record CreateCharacterDto(
    string Name,
    string Class,
    string? Description = null);

public record UpdateCharacterDto(
    string Name,
    string Class,
    string? Description);

public record ClassUpgradeOptionDto(
    int Id,
    string Name,
    string Archetype,
    int Tier,
    int RequiredLevel,
    bool IsAdvanced,
    int Branch,
    int BaseStrength,
    int BaseAgility,
    int BaseIntelligence,
    int BaseWisdom,
    int BaseCharisma,
    int BaseEndurance,
    int BaseLuck,
    List<ClassUpgradeSkillDto> Skills);

public record ClassUpgradeSkillDto(
    int Id,
    string Name,
    SkillType Type,
    int RequiredLevel,
    int ManaCost,
    int StaminaCost,
    int Cooldown,
    int AttackPower,
    int DefensePower,
    int SpeedModifier,
    int MagicPower,
    ElementType Element,
    double ElementPowerMultiplier);

public record UpgradeClassDto(string ClassName);

public record UpsertLoadoutDto(string Name, List<int> ActiveSkillOrder, List<int> PassiveSkillIds);
}
