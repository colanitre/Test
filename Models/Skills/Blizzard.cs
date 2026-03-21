namespace RpgApi.Models;

public class Blizzard : Skill
{
    public Blizzard() : base()
    {
        Name = "Blizzard";
        ManaCost = 30;
        StaminaCost = 10;
        Cooldown = 5;
        RequiredLevel = 6;
        RequiredIntelligence = 16;
        RequiredWisdom = 12;
        Description = "Unleash a devastating blizzard that damages and slows enemies.";
        Type = SkillType.Active;
        AttackPower = 25;
        DefensePower = 0;
        SpeedModifier = 2;
        MagicPower = 70;
    }
}
