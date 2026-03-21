namespace RpgApi.Models;

public class RippleStrike : Skill
{
    public RippleStrike() : base()
    {
        Name = "Ripple Strike";
        ManaCost = 10;
        StaminaCost = 12;
        Cooldown = 1;
        RequiredLevel = 2;
        RequiredAgility = 11;
        RequiredStrength = 12;
        Description = "Strike your enemy, sending ripples of energy that extend the damage.";
        Type = SkillType.Active;
        AttackPower = 48;
        DefensePower = 0;
        SpeedModifier = 3;
        MagicPower = 15;
    }
}
