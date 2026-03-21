namespace RpgApi.Models;

public class QuickShot : Skill
{
    public QuickShot() : base()
    {
        Name = "Quick Shot";
        Description = "An instant arrow shot with no cooldown for rapid fire.";
        Cooldown = 0;
        RequiredLevel = 1;
        RequiredAgility = 12;
        Type = SkillType.Active;
        AttackPower = 14;
        DefensePower = 0;
        SpeedModifier = 5;
        MagicPower = 0;
    }
}