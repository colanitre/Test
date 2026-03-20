using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers;

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

        var character = new Character
        {
            Name = createCharacterDto.Name,
            Class = createCharacterDto.Class,
            Level = createCharacterDto.Level ?? 1,
            Health = createCharacterDto.Health ?? 100,
            Mana = createCharacterDto.Mana ?? 50,
            Experience = createCharacterDto.Experience ?? 0,
            Description = createCharacterDto.Description,
            PlayerId = playerId
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
            .FirstOrDefaultAsync(c => c.Id == id && c.PlayerId == playerId);

        if (character == null)
        {
            _logger.LogWarning("Character with ID {CharacterId} for player {PlayerId} not found", id, playerId);
            return NotFound(new { message = "Character not found" });
        }

        character.Name = updateCharacterDto.Name;
        character.Class = updateCharacterDto.Class;
        character.Level = updateCharacterDto.Level;
        character.Health = updateCharacterDto.Health;
        character.Mana = updateCharacterDto.Mana;
        character.Experience = updateCharacterDto.Experience;
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
            character.Class,
            character.Level,
            character.Health,
            character.Mana,
            character.Experience,
            character.Description,
            character.CreatedAt,
            character.UpdatedAt,
            character.PlayerId);
}

public record CharacterDetailDto(
    int Id,
    string Name,
    string Class,
    int Level,
    int Health,
    int Mana,
    int Experience,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int PlayerId);

public record CreateCharacterDto(
    string Name,
    string Class,
    int? Level = null,
    int? Health = null,
    int? Mana = null,
    int? Experience = null,
    string? Description = null);

public record UpdateCharacterDto(
    string Name,
    string Class,
    int Level,
    int Health,
    int Mana,
    int Experience,
    string? Description);
