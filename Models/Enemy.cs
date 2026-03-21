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

        // Initialize stats from the enemy class
        MaxHealth = enemyClass.BaseStrength * enemyClass.BaseEndurance;
        Health = MaxHealth;
        MaxMana = enemyClass.BaseIntelligence * enemyClass.BaseWisdom;
        Mana = MaxMana;
        MaxStamina = enemyClass.BaseEndurance * enemyClass.BaseAgility;
        Stamina = MaxStamina;
        Attack = enemyClass.BaseStrength * enemyClass.BaseAgility;
        Defense = enemyClass.BaseEndurance * enemyClass.BaseStrength;
        Speed = enemyClass.BaseAgility * 2;
        Magic = enemyClass.BaseIntelligence * enemyClass.BaseWisdom;

        Skills = enemyClass.Skills?.ToList() ?? new List<Skill>();
        ExperienceReward = Level * 10;
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