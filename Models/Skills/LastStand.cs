namespace RpgApi.Models;

public class LastStand : Skill
{
    public LastStand() : base()
    {
        Name = "Last Stand";
        ManaCost = 0;
        StaminaCost = 30;
        Cooldown = 6;
        RequiredLevel = 6;
        RequiredEndurance = 17;
        RequiredStrength = 15;
        Description = "Plant yourself and take reduced damage while dealing massive counterattacks.";
        Type = SkillType.Active;
        AttackPower = 70;
        DefensePower = 90;
        SpeedModifier = 0;
        MagicPower = 0;
    }
}
