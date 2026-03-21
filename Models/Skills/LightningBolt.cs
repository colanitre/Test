namespace RpgApi.Models;

public class LightningBolt : Skill
{
    public LightningBolt() : base()
    {
        Name = "Lightning Bolt";
        ManaCost = 20;
        StaminaCost = 5;
        Cooldown = 2;
        RequiredLevel = 4;
        RequiredIntelligence = 14;
        RequiredWisdom = 10;
        Description = "Strike with electricity from the heavens, dealing magic damage and stunning the enemy.";
        Type = SkillType.Active;
        AttackPower = 20;
        DefensePower = 0;
        SpeedModifier = 3;
        MagicPower = 60;
        Element = ElementType.Lightning;
        ElementPowerMultiplier = 1.15;
    }
}
