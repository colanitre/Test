namespace RpgApi.Models;

/// <summary>
/// Represents a player in the RPG system
/// </summary>
public class Player
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public ICollection<Character> Characters { get; set; } = [];
}
