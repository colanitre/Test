namespace RpgApi.Models;

public class Volley : Skill
{
    public Volley() : base()
    {
        Name = "Volley";
        ManaCost = 0;
        StaminaCost = 18;
        Cooldown = 2;
        RequiredLevel = 4;
        RequiredAgility = 15;
        Description = "Fire a barrage of arrows in rapid succession at the enemy.";
        Type = SkillType.Active;
        AttackPower = 55;
        DefensePower = 0;
        SpeedModifier = 5;
        MagicPower = 0;
    }
}
