namespace RpgApi.Models;

/// <summary>
/// Represents an enemy character that players can fight
/// </summary>
public class Enemy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int Stamina { get; set; }
    public int MaxStamina { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Magic { get; set; }
    public List<Skill> Skills { get; set; } = [];
    public int ExperienceReward { get; set; } = 10;
    public string? Description { get; set; }
    public EnemyType Type { get; set; } = EnemyType.Common;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key for EnemyClass
    public int EnemyClassId { get; set; }

    // Navigation property
    public EnemyClass? EnemyClass { get; set; }

    // Constructor to initialize stats from enemy class
    public Enemy()
    {

    }

    public Enemy(string name, EnemyClass enemyClass)
    {
        Name = name ?? string.Empty;
        EnemyClass = enemyClass;
        EnemyClassId = enemyClass.Id;
        Level = enemyClass.BaseLevel;
        Type = enemyClass.Type;

        // Calculate difficulty multiplier based on enemy type
        double difficultyMultiplier = Type switch
        {
            EnemyType.Common => 1.0,
            EnemyType.Elite => 2.0,
            EnemyType.Boss => 3.0,
            EnemyType.Legendary => 4.0,
            _ => 1.0
        };

        // Initialize stats from the enemy class with difficulty multiplier
        MaxHealth = (int)(enemyClass.BaseStrength * enemyClass.BaseEndurance * difficultyMultiplier);
        Health = MaxHealth;
        MaxMana = (int)(enemyClass.BaseIntelligence * enemyClass.BaseWisdom * difficultyMultiplier);
        Mana = MaxMana;
        MaxStamina = (int)(enemyClass.BaseEndurance * enemyClass.BaseAgility * difficultyMultiplier);
        Stamina = MaxStamina;
        Attack = (int)(enemyClass.BaseStrength * enemyClass.BaseAgility * difficultyMultiplier);
        Defense = (int)(enemyClass.BaseEndurance * enemyClass.BaseStrength * difficultyMultiplier);
        Speed = (int)(enemyClass.BaseAgility * 2 * difficultyMultiplier);
        Magic = (int)(enemyClass.BaseIntelligence * enemyClass.BaseWisdom * difficultyMultiplier);

        Skills = enemyClass.Skills?.ToList() ?? new List<Skill>();
        ExperienceReward = (int)(Level * 10 * difficultyMultiplier);
        Description = enemyClass.Description;
    }
}

public enum EnemyType
{
    Common,
    Elite,
    Boss,
    Legendary
}