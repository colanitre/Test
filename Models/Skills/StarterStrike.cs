namespace RpgApi.Models;

public class StarterStrike : Skill
{
    public StarterStrike() : base()
    {
        Name = "Starter Strike";
        Description = "A simple low-cost attack available from the start.";
        Cooldown = 0;
        RequiredLevel = 0;
        Type = SkillType.Active;
        ManaCost = 0;
        StaminaCost = 0;
        AttackPower = 6;
        DefensePower = 0;
        SpeedModifier = 1;
        MagicPower = 0;
    }
}
