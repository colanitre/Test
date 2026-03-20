namespace RpgApi.Models;

/// <summary>
/// Represents a character belonging to a player
/// </summary>
public class Character
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 0;
    public int Health { get; set; }
    public int Mana { get; set; }
    public int Stamina { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Magic { get; set; }
    public List<Skill> Skills { get; set; } = [];
    public int Experience { get; set; } = 0;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Foreign key for Player
    public int PlayerId { get; set; }

    // Foreign key for Class
    public int ClassId { get; set; }

    // Navigation properties
    public Player? Player { get; set; }
    public Class? Class { get; set; }

    // Constructor to initialize stats from class
    public Character()
    {

    }

    public Character(string name, Class characterClass, int playerId)
    {
        Name = name ?? string.Empty;
        Class = characterClass;
        ClassId = characterClass.Id;
        PlayerId = playerId;
        // Initialize stats from the character class
        Health = characterClass.BaseStrength * characterClass.BaseEndurance; // Assuming health is based on strength and endurance for simplicity
        Mana = characterClass.BaseIntelligence * characterClass.BaseWisdom; // Assuming mana is based on intelligence for simplicity
        Stamina = characterClass.BaseEndurance * characterClass.BaseAgility; // Assuming stamina is based on endurance and agility for simplicity
        Attack = characterClass.BaseStrength * characterClass.BaseAgility; // Assuming attack is based on strength and agility for simplicity
        Defense = characterClass.BaseEndurance * characterClass.BaseAgility; // Assuming defense is based on endurance and agility for simplicity
        Speed = characterClass.BaseAgility*characterClass.BaseStrength ; // Assuming speed is based on agility for simplicity
        Magic = characterClass.BaseIntelligence * characterClass.BaseCharisma; // Assuming magic is based on intelligence and charisma for simplicity
        Skills = characterClass.Skills.ToList(); // Initialize with the list of skills from the character class
    }
}