using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly RpgContext _context;
    private readonly ILogger<PlayersController> _logger;

    public PlayersController(RpgContext context, ILogger<PlayersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all players
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlayerDto>>> GetPlayers()
    {
        _logger.LogInformation("Fetching all players");
        var players = await _context.Players
            .Include(p => p.Characters)
            .ToListAsync();
        
        return Ok(players.Select(ToDto));
    }

    /// <summary>
    /// Get a specific player by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PlayerDto>> GetPlayer(int id)
    {
        _logger.LogInformation("Fetching player with ID: {PlayerId}", id);
        var player = await _context.Players
            .Include(p => p.Characters)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (player == null)
        {
            _logger.LogWarning("Player with ID {PlayerId} not found", id);
            return NotFound(new { message = "Player not found" });
        }

        return Ok(ToDto(player));
    }

    /// <summary>
    /// Create a new player
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PlayerDto>> CreatePlayer([FromBody] CreatePlayerDto createPlayerDto)
    {
        _logger.LogInformation("Creating new player: {Username}", createPlayerDto.Username);

        // Check if username already exists
        if (await _context.Players.AnyAsync(p => p.Username == createPlayerDto.Username))
        {
            _logger.LogWarning("Username '{Username}' already exists", createPlayerDto.Username);
            return BadRequest(new { message = "Username already exists" });
        }

        // Check if email already exists
        if (await _context.Players.AnyAsync(p => p.Email == createPlayerDto.Email))
        {
            _logger.LogWarning("Email '{Email}' already exists", createPlayerDto.Email);
            return BadRequest(new { message = "Email already exists" });
        }

        var player = new Player
        {
            Username = createPlayerDto.Username,
            Email = createPlayerDto.Email
        };

        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player created successfully with ID: {PlayerId}", player.Id);
        return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, ToDto(player));
    }

    /// <summary>
    /// Update an existing player
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlayer(int id, [FromBody] UpdatePlayerDto updatePlayerDto)
    {
        _logger.LogInformation("Updating player with ID: {PlayerId}", id);
        var player = await _context.Players.FindAsync(id);

        if (player == null)
        {
            _logger.LogWarning("Player with ID {PlayerId} not found for update", id);
            return NotFound(new { message = "Player not found" });
        }

        // Check if new username exists (if changed)
        if (player.Username != updatePlayerDto.Username && 
            await _context.Players.AnyAsync(p => p.Username == updatePlayerDto.Username))
        {
            _logger.LogWarning("Username '{Username}' already exists", updatePlayerDto.Username);
            return BadRequest(new { message = "Username already exists" });
        }

        // Check if new email exists (if changed)
        if (player.Email != updatePlayerDto.Email && 
            await _context.Players.AnyAsync(p => p.Email == updatePlayerDto.Email))
        {
            _logger.LogWarning("Email '{Email}' already exists", updatePlayerDto.Email);
            return BadRequest(new { message = "Email already exists" });
        }

        player.Username = updatePlayerDto.Username;
        player.Email = updatePlayerDto.Email;
        player.UpdatedAt = DateTime.UtcNow;

        _context.Players.Update(player);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player with ID {PlayerId} updated successfully", id);
        return NoContent();
    }

    /// <summary>
    /// Delete a player
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlayer(int id)
    {
        _logger.LogInformation("Deleting player with ID: {PlayerId}", id);
        var player = await _context.Players.FindAsync(id);

        if (player == null)
        {
            _logger.LogWarning("Player with ID {PlayerId} not found for deletion", id);
            return NotFound(new { message = "Player not found" });
        }

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Player with ID {PlayerId} deleted successfully", id);
        return NoContent();
    }

    private static PlayerDto ToDto(Player player)
    {
        var characters = player.Characters.Select(c => new CharacterDto(
            c.Id,
            c.Name,
            c.Class?.Name ?? "Unknown",
            c.Level,
            c.Health,
            c.Mana,
            c.Experience,
            c.Description,
            c.CreatedAt,
            c.UpdatedAt)).ToList();

        return new PlayerDto(
            player.Id,
            player.Username,
            player.Email,
            player.CreatedAt,
            player.UpdatedAt,
            characters);
    }
}

public record PlayerDto(
    int Id,
    string Username,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<CharacterDto> Characters);

public record CreatePlayerDto(
    string Username,
    string Email);

public record UpdatePlayerDto(
    string Username,
    string Email);

public record CharacterDto(
    int Id,
    string Name,
    string Class,
    int Level,
    int Health,
    int Mana,
    int Experience,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
