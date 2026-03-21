namespace RpgApi.Models;

public class DamageCurveConfig
{
    public double CharacterAttackWeight { get; set; } = 1.2;
    public double CharacterMagicWeight { get; set; } = 1.1;
    public double CharacterSpeedWeight { get; set; } = 0.5;
    public double SkillAttackWeight { get; set; } = 1.6;
    public double SkillMagicWeight { get; set; } = 1.8;
    public double SkillSpeedWeight { get; set; } = 0.4;
    public double EnemyDefenseWeight { get; set; } = 1.0;
    public double EnemyMagicDefenseWeight { get; set; } = 0.7;
    public double EnemyStaminaDefenseWeight { get; set; } = 0.3;

    public double EnemyAttackWeight { get; set; } = 1.1;
    public double EnemyMagicWeight { get; set; } = 1.0;
    public double EnemySpeedWeight { get; set; } = 0.4;
    public double CharacterDefenseWeight { get; set; } = 1.0;
    public double CharacterMagicDefenseWeight { get; set; } = 0.7;
    public double CharacterStaminaDefenseWeight { get; set; } = 0.2;

    public int MinimumDamage { get; set; } = 1;
}