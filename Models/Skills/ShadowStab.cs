namespace RpgApi.Models;

public class ShadowStab : Skill
{
    public ShadowStab() : base()
    {
        Name = "Shadow Stab";
        Description = "A quick, precise strike from the shadows.";
        Cooldown = 0;
        RequiredLevel = 1;
        RequiredAgility = 12;
        Type = SkillType.Active;
        AttackPower = 18;
        DefensePower = 2;
        SpeedModifier = 5;
        MagicPower = 0;
    }
}