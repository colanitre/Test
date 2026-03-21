namespace RpgApi.Models;

public class Windslash : Skill
{
    public Windslash() : base()
    {
        Name = "Windslash";
        ManaCost = 12;
        StaminaCost = 8;
        Cooldown = 2;
        RequiredLevel = 3;
        RequiredAgility = 15;
        RequiredStrength = 11;
        Description = "Strike with the speed of the wind, dealing high damage with increased speed.";
        Type = SkillType.Active;
        AttackPower = 55;
        DefensePower = 0;
        SpeedModifier = 6;
        MagicPower = 10;
    }
}
