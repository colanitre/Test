namespace RpgApi.Models;

public class DeadlyPoison : Skill
{
    public DeadlyPoison() : base()
    {
        Name = "Deadly Poison";
        ManaCost = 5;
        StaminaCost = 10;
        Cooldown = 4;
        RequiredLevel = 3;
        RequiredAgility = 14;
        RequiredIntelligence = 10;
        Description = "Coat your blade with deadly poison, dealing high damage over time.";
        Type = SkillType.Active;
        AttackPower = 60;
        DefensePower = 0;
        SpeedModifier = 4;
        MagicPower = 20;
    }
}
