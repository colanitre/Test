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
                character.Class.BaseStrength,
                character.Class.BaseAgility,
                character.Class.BaseIntelligence,
                character.Class.BaseWisdom,
                character.Class.BaseCharisma,
                character.Class.BaseEndurance,
                character.Class.BaseLuck) : null,
            character.Level,
            character.Health,
            character.Mana,
            character.Experience,
            character.Description,
            character.CreatedAt,
            character.UpdatedAt,
            character.PlayerId,
            character.Skills.Select(s => new SkillDto(
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

public record CharacterDetailDto(
    int Id,
    string Name,
    ClassDto? Class,
    int Level,
    int Health,
    int Mana,
    int Experience,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int PlayerId,
    List<SkillDto> Skills);

public record ClassDto(
    int Id,
    string Name,
    int BaseStrength,
    int BaseAgility,
    int BaseIntelligence,
    int BaseWisdom,
    int BaseCharisma,
    int BaseEndurance,
    int BaseLuck);

public record SkillDto(
    int Id,
    string Name,
    string Description,
    SkillType Type,
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
}
