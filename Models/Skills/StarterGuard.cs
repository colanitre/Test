namespace RpgApi.Models;

public class StarterGuard : Skill
{
    public StarterGuard() : base()
    {
        Name = "Starter Guard";
        Description = "A basic defensive stance that is always available.";
        Cooldown = 0;
        RequiredLevel = 0;
        Type = SkillType.Passive;
        ManaCost = 0;
        StaminaCost = 0;
        AttackPower = 0;
        DefensePower = 6;
        SpeedModifier = 0;
        MagicPower = 0;
    }
}
