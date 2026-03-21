namespace RpgApi.Models;

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SkillType Type { get; set; } = SkillType.Active;
    public int Level { get; set; } = 1;
    public int ManaCost { get; set; } = 0;
    public int StaminaCost { get; set; } = 0;
    public int Cooldown { get; set; } = 0;
    public int RequiredLevel { get; set; } = 1;
    public int RequiredStrength { get; set; } = 0;
    public int RequiredAgility { get; set; } = 0;
    public int RequiredIntelligence { get; set; } = 0;
    public int RequiredWisdom { get; set; } = 0;
    public int RequiredCharisma { get; set; } = 0;
    public int RequiredEndurance { get; set; } = 0;
    public int RequiredLuck { get; set; } = 0;  

    // base stats
    public int StrengthModifier { get; set; } = 1;
    public int AgilityModifier { get; set; } = 1;
    public int IntelligenceModifier { get; set; } = 1;
    public int WisdomModifier { get; set; } = 1;
    public int CharismaModifier { get; set; } = 1;
    public int EnduranceModifier { get; set; } = 1;
    public int LuckModifier { get; set; } = 1;

    public int AttackPower { get; set; } = 1;
    public int DefensePower { get; set; } = 1;
    public int SpeedModifier { get; set; } = 1;
    public int MagicPower { get; set; } = 1;
    public ElementType Element { get; set; } = ElementType.Physical;
    public double ElementPowerMultiplier { get; set; } = 1.0;

    // Navigation properties
    public ICollection<Class> Classes { get; set; } = [];
    public ICollection<EnemyClass> EnemyClasses { get; set; } = [];
}

public enum SkillType
{
    Active,
    Passive,
    Ultimate
}

public enum ElementType
{
    Physical,
    Fire,
    Ice,
    Lightning,
    Poison,
    Holy,
    Shadow,
    Arcane
}