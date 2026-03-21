namespace RpgApi.Models;

public class Meteor : Skill
{
    public Meteor() : base()
    {
        Name = "Meteor";
        ManaCost = 40;
        StaminaCost = 15;
        Cooldown = 5;
        RequiredLevel = 7;
        RequiredIntelligence = 17;
        RequiredWisdom = 13;
        Description = "Call meteors down from the sky to devastate a wide area.";
        Type = SkillType.Active;
        AttackPower = 35;
        DefensePower = 0;
        SpeedModifier = 1;
        MagicPower = 85;
        Element = ElementType.Fire;
        ElementPowerMultiplier = 1.25;
    }
}
