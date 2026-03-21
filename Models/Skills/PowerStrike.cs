namespace RpgApi.Models;

public class PowerStrike : Skill
{
    public PowerStrike() : base()
    {
        Name = "Power Strike";
        Description = "A heavy strike with no cooldown to use whenever you need extra damage.";
        Cooldown = 0;
        RequiredLevel = 1;
        RequiredStrength = 14;
        Type = SkillType.Active;
        AttackPower = 20;
        DefensePower = 0;
        SpeedModifier = 0;
        MagicPower = 0;
    }
}