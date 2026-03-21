namespace RpgApi.Models;

public class ArcaneSpark : Skill
{
    public ArcaneSpark() : base()
    {
        Name = "Arcane Spark";
        Description = "A small instant burst of arcane energy.";
        Cooldown = 0;
        RequiredLevel = 0;
        RequiredIntelligence = 10;
        Type = SkillType.Active;
        AttackPower = 12;
        DefensePower = 0;
        SpeedModifier = 3;
        MagicPower = 15;
        Element = ElementType.Arcane;
        ElementPowerMultiplier = 1.05;
    }
}