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

        RecalculateDerivedStats();
    }

    public void RecalculateDerivedStats()
    {
        if (EnemyClass == null)
        {
            return;
        }

        // Calculate difficulty multiplier based on enemy type
        double difficultyMultiplier = Type switch
        {
            EnemyType.Common => 1.0,
            EnemyType.Elite => 1.25,
            EnemyType.Boss => 1.5,
            EnemyType.Legendary => 2.0,
            EnemyType.Godlike => 3.0,
            _ => 1.0
        };

        var effectiveLevel = Math.Max(1, Level);
        var baseLevel = Math.Max(1, EnemyClass.BaseLevel);

        // Scale safely both above and below base level without ever going negative.
        double levelMultiplier = effectiveLevel >= baseLevel
            ? 1.0 + ((effectiveLevel - baseLevel) * 0.02)
            : Math.Max(0.25, 1.0 - ((baseLevel - effectiveLevel) * 0.01));

        // Initialize stats from the enemy class with difficulty and level multipliers
        MaxHealth = Math.Max(1, (int)(EnemyClass.BaseStrength * EnemyClass.BaseEndurance * difficultyMultiplier * levelMultiplier));
        Health = MaxHealth;
        MaxMana = Math.Max(0, (int)(EnemyClass.BaseIntelligence * EnemyClass.BaseWisdom * difficultyMultiplier * levelMultiplier));
        Mana = MaxMana;
        MaxStamina = Math.Max(0, (int)(EnemyClass.BaseEndurance * EnemyClass.BaseAgility * difficultyMultiplier * levelMultiplier));
        Stamina = MaxStamina;
        HealthRegen = Math.Max(0, (int)(EnemyClass.BaseEndurance * difficultyMultiplier * levelMultiplier / 5));
        ManaRegen = Math.Max(0, (int)(EnemyClass.BaseWisdom * difficultyMultiplier * levelMultiplier / 5));
        StaminaRegen = Math.Max(0, (int)(EnemyClass.BaseAgility * difficultyMultiplier * levelMultiplier / 5));
        Attack = Math.Max(1, (int)((EnemyClass.BaseStrength * EnemyClass.BaseAgility + EnemyClass.BaseIntelligence * EnemyClass.BaseWisdom) * difficultyMultiplier * levelMultiplier));
        Defense = Math.Max(1, (int)((EnemyClass.BaseEndurance * EnemyClass.BaseStrength + EnemyClass.BaseAgility * EnemyClass.BaseIntelligence) * difficultyMultiplier * levelMultiplier));
        Speed = Math.Max(1, (int)(EnemyClass.BaseAgility * 2 * difficultyMultiplier * levelMultiplier));
        Magic = Math.Max(0, (int)(EnemyClass.BaseIntelligence * EnemyClass.BaseWisdom * difficultyMultiplier * levelMultiplier));

        Skills = EnemyClass.Skills?.ToList() ?? new List<Skill>();
        var highLevelXpScale = effectiveLevel <= 50
            ? 1.0
            : 1.0 + ((effectiveLevel - 50) * 0.015);

        var rawExperienceReward = effectiveLevel * 10.0 * difficultyMultiplier * highLevelXpScale;
        ExperienceReward = rawExperienceReward >= int.MaxValue
            ? int.MaxValue
            : Math.Max(1, (int)Math.Round(rawExperienceReward));
        Description = EnemyClass.Description;
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
    Legendary,
    Godlike
}