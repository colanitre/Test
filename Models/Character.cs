namespace RpgApi.Models;

/// <summary>
/// Represents a character belonging to a player
/// </summary>
public class Character
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Class { get; set; }
    public int Level { get; set; } = 1;
    public int Health { get; set; } = 100;
    public int Mana { get; set; } = 50;
    public int Experience { get; set; } = 0;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Foreign key
    public int PlayerId { get; set; }

    // Navigation property
    public Player? Player { get; set; }
}
