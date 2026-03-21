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
        Level = 1;
        Skills = characterClass.Skills.ToList();
        RecalculateDerivedStats();
    }

    public void RecalculateDerivedStats(bool restoreResourcesToFull = true)
    {
        if (Class == null)
        {
            return;
        }

        Level = Math.Max(1, Level);
        var extraLevels = Level - 1;

        MaxHealth = (Class.BaseStrength * Class.BaseEndurance) + (10 * extraLevels);
        MaxMana = (Class.BaseIntelligence * Class.BaseWisdom) + (5 * extraLevels);
        MaxStamina = (Class.BaseEndurance * Class.BaseAgility) + (5 * extraLevels);
        HealthRegen = Class.BaseEndurance / 5;
        ManaRegen = Class.BaseWisdom / 5;
        StaminaRegen = Class.BaseAgility / 5;
        Attack = (Class.BaseStrength * Class.BaseAgility) + (Class.BaseIntelligence * Class.BaseCharisma) + (2 * extraLevels);
        Defense = (Class.BaseEndurance * Class.BaseAgility) + (Class.BaseAgility * Class.BaseIntelligence) + (2 * extraLevels);
        Speed = (Class.BaseAgility * Class.BaseStrength) + extraLevels;
        Magic = (Class.BaseIntelligence * Class.BaseCharisma) + extraLevels;

        if (restoreResourcesToFull)
        {
            Health = MaxHealth;
            Mana = MaxMana;
            Stamina = MaxStamina;
            return;
        }

        Health = Math.Min(Math.Max(Health, 0), MaxHealth);
        Mana = Math.Min(Math.Max(Mana, 0), MaxMana);
        Stamina = Math.Min(Math.Max(Stamina, 0), MaxStamina);
    }

    public void Regenerate()
    {
        Health = Math.Min(Health + HealthRegen, MaxHealth);
        Mana = Math.Min(Mana + ManaRegen, MaxMana);
        Stamina = Math.Min(Stamina + StaminaRegen, MaxStamina);
    }
}