namespace RpgApi.Models;

public class ChainLightning : Skill
{
    public ChainLightning() : base()
    {
        Name = "Chain Lightning";
        ManaCost = 28;
        StaminaCost = 8;
        Cooldown = 3;
        RequiredLevel = 6;
        RequiredIntelligence = 15;
        RequiredWisdom = 11;
        Description = "Lightning that chains between targets, hitting multiple times with increasing power.";
        Type = SkillType.Active;
        AttackPower = 30;
        DefensePower = 0;
        SpeedModifier = 4;
        MagicPower = 65;
        Element = ElementType.Lightning;
        ElementPowerMultiplier = 1.2;
    }
}
