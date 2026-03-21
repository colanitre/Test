namespace RpgApi.Models;

public class HolyLight : Skill
{
    public HolyLight() : base()
    {
        Name = "Holy Light";
        ManaCost = 20;
        StaminaCost = 0;
        Cooldown = 5;
        RequiredLevel = 4;
        RequiredWisdom = 16;
        RequiredCharisma = 12;
        Description = "Call upon holy power to damage enemies and heal the user slightly.";
        Type = SkillType.Active;
        AttackPower = 30;
        DefensePower = 10;
        SpeedModifier = 1;
        MagicPower = 55;
    }
}
