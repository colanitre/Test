namespace RpgApi.Models;

public class MultiShot : Skill
{
    public MultiShot() : base()
    {
        Name = "Multi-Shot";
        ManaCost = 0;
        StaminaCost = 15;
        Cooldown = 1;
        RequiredLevel = 2;
        RequiredAgility = 14;
        Description = "Fire multiple arrows in rapid succession, dealing consistent damage.";
        Type = SkillType.Active;
        AttackPower = 45;
        DefensePower = 0;
        SpeedModifier = 5;
        MagicPower = 0;
    }
}
