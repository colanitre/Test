namespace RpgApi.Models;

public class Whirlwind : Skill
{
    public Whirlwind() : base()
    {
        Name = "Whirlwind";
        ManaCost = 0;
        StaminaCost = 25;
        Cooldown = 4;
        RequiredLevel = 5;
        RequiredStrength = 15;
        RequiredAgility = 13;
        Description = "Spin rapidly, striking all enemies around you multiple times.";
        Type = SkillType.Active;
        AttackPower = 50;
        DefensePower = 0;
        SpeedModifier = 7;
        MagicPower = 0;
    }
}
