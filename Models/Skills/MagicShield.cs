namespace RpgApi.Models;

public class MagicShield : Skill
{
    public MagicShield() : base()
    {
        Name = "Magic Shield";
        ManaCost = 20;
        StaminaCost = 5;
        Cooldown = 4;
        RequiredLevel = 3;
        RequiredWisdom = 12;
        RequiredIntelligence = 11;
        Description = "Create a protective magical barrier that reduces damage taken.";
        Type = SkillType.Active;
        AttackPower = 0;
        DefensePower = 70;
        SpeedModifier = 0;
        MagicPower = 40;
    }
}
