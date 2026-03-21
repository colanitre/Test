namespace RpgApi.Models;

public class DamageCurveConfig
{
    public double CharacterAttackWeight { get; set; } = 1.0;
    public double CharacterMagicWeight { get; set; } = 1.0;
    public double CharacterSpeedWeight { get; set; } = 1.0;
    public double SkillAttackWeight { get; set; } = 1.0;
    public double SkillMagicWeight { get; set; } = 1.0;
    public double SkillSpeedWeight { get; set; } = 1.0;
    public double EnemyDefenseWeight { get; set; } = 1.0;
    public double EnemyMagicDefenseWeight { get; set; } = 1.0;
    public double EnemyStaminaDefenseWeight { get; set; } = 1.0;

    public double EnemyAttackWeight { get; set; } = 1.0;
    public double EnemyMagicWeight { get; set; } = 1.0;
    public double EnemySpeedWeight { get; set; } = 1.0;
    public double CharacterDefenseWeight { get; set; } = 1.0;
    public double CharacterMagicDefenseWeight { get; set; } = 1.0;
    public double CharacterStaminaDefenseWeight { get; set; } = 1.0;

    public int MinimumDamage { get; set; } = 5;
}