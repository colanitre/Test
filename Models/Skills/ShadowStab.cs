namespace RpgApi.Models;

public class ShadowStab : Skill
{
    public ShadowStab() : base()
    {
        Name = "Shadow Stab";
        Description = "A quick, precise strike from the shadows.";
        Cooldown = 0;
        RequiredLevel = 0;
        RequiredAgility = 12;
        Type = SkillType.Active;
        AttackPower = 35;
        DefensePower = 4;
        SpeedModifier = 8;
        MagicPower = 0;
    }
}