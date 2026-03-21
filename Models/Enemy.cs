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
    public int HealthRegen { get; set; }
    public int ManaRegen { get; set; }
    public int StaminaRegen { get; set; }
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

    public Enemy(string name, EnemyClass enemyClass, int? level = null)
    {
        Name = name ?? string.Empty;
        EnemyClass = enemyClass;
        EnemyClassId = enemyClass.Id;
        Level = level ?? enemyClass.BaseLevel;
        Type = enemyClass.Type;

        // Calculate difficulty multiplier based on enemy type
        double difficultyMultiplier = Type switch
        {
            EnemyType.Common => 1.0,
            EnemyType.Elite => 1.25,
            EnemyType.Boss => 1.5,
            EnemyType.Legendary => 2.0,
            _ => 1.0
        };

        // Apply level scaling: +2% per level above base level
        double levelMultiplier = 1.0 + (Level - enemyClass.BaseLevel) * 0.02;

        // Initialize stats from the enemy class with difficulty and level multipliers
        MaxHealth = (int)(enemyClass.BaseStrength * enemyClass.BaseEndurance * difficultyMultiplier * levelMultiplier);
        Health = MaxHealth;
        MaxMana = (int)(enemyClass.BaseIntelligence * enemyClass.BaseWisdom * difficultyMultiplier * levelMultiplier);
        Mana = MaxMana;
        MaxStamina = (int)(enemyClass.BaseEndurance * enemyClass.BaseAgility * difficultyMultiplier * levelMultiplier);
        Stamina = MaxStamina;
        HealthRegen = (int)(enemyClass.BaseEndurance * difficultyMultiplier * levelMultiplier / 5);
        ManaRegen = (int)(enemyClass.BaseWisdom * difficultyMultiplier * levelMultiplier / 5);
        StaminaRegen = (int)(enemyClass.BaseAgility * difficultyMultiplier * levelMultiplier / 5);
        Attack = (int)((enemyClass.BaseStrength * enemyClass.BaseAgility + enemyClass.BaseIntelligence * enemyClass.BaseWisdom) * difficultyMultiplier * levelMultiplier);
        Defense = (int)((enemyClass.BaseEndurance * enemyClass.BaseStrength + enemyClass.BaseAgility * enemyClass.BaseIntelligence) * difficultyMultiplier * levelMultiplier);
        Speed = (int)(enemyClass.BaseAgility * 2 * difficultyMultiplier * levelMultiplier);
        Magic = (int)(enemyClass.BaseIntelligence * enemyClass.BaseWisdom * difficultyMultiplier * levelMultiplier);

        Skills = enemyClass.Skills?.ToList() ?? new List<Skill>();
        ExperienceReward = (int)(Level * 10 * difficultyMultiplier);
        Description = enemyClass.Description;
    }

    public void Regenerate()
    {
        Health = Math.Min(Health + HealthRegen, MaxHealth);
        Mana = Math.Min(Mana + ManaRegen, MaxMana);
        Stamina = Math.Min(Stamina + StaminaRegen, MaxStamina);
    }
}

public enum EnemyType
{
    Common,
    Elite,
    Boss,
    Legendary
}