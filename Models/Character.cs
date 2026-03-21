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
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int Stamina { get; set; }
    public int MaxStamina { get; set; }
    public int HealthRegen { get; set; }
    public int ManaRegen { get; set; }
    public int StaminaRegen { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Magic { get; set; }
    public List<Skill> Skills { get; set; } = [];
    public int Experience { get; set; } = 0;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int? AdvancedPath { get; set; }

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
        MaxHealth = characterClass.BaseStrength * characterClass.BaseEndurance; // Assuming health is based on strength and endurance for simplicity
        Health = MaxHealth;
        MaxMana = characterClass.BaseIntelligence * characterClass.BaseWisdom; // Assuming mana is based on intelligence for simplicity
        Mana = MaxMana;
        MaxStamina = characterClass.BaseEndurance * characterClass.BaseAgility; // Assuming stamina is based on endurance and agility for simplicity
        Stamina = MaxStamina;
        HealthRegen = characterClass.BaseEndurance / 5;
        ManaRegen = characterClass.BaseWisdom / 5;
        StaminaRegen = characterClass.BaseAgility / 5;
        Attack = (characterClass.BaseStrength * characterClass.BaseAgility) + (characterClass.BaseIntelligence * characterClass.BaseCharisma);
        Defense = (characterClass.BaseEndurance * characterClass.BaseAgility) + (characterClass.BaseAgility * characterClass.BaseIntelligence);
        Speed = characterClass.BaseAgility * characterClass.BaseStrength;
        Magic = characterClass.BaseIntelligence * characterClass.BaseCharisma;
        Skills = characterClass.Skills.ToList(); // Initialize with the list of skills from the character class
    }

    public void Regenerate()
    {
        Health = Math.Min(Health + HealthRegen, MaxHealth);
        Mana = Math.Min(Mana + ManaRegen, MaxMana);
        Stamina = Math.Min(Stamina + StaminaRegen, MaxStamina);
    }
}