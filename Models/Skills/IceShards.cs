namespace RpgApi.Models;

public class IceShards : Skill
{
    public IceShards() : base()
    {
        Name = "Ice Shards";
        ManaCost = 15;
        StaminaCost = 5;
        Cooldown = 2;
        RequiredLevel = 5;
        RequiredIntelligence = 12;
        RequiredWisdom = 8;
        Description = "Fire shards of ice at the enemy, dealing moderate magic damage.";
        Type = SkillType.Active;
        AttackPower = 20;
        DefensePower = 0;
        SpeedModifier = 3;
        MagicPower = 40;
        Element = ElementType.Ice;
        ElementPowerMultiplier = 1.15;
    }
}
