namespace RpgApi.Models;

public class Cleave : Skill
{
    public Cleave() : base()
    {
        Name = "Cleave";
        ManaCost = 0;
        StaminaCost = 20;
        Cooldown = 2;
        RequiredLevel = 2;
        RequiredStrength = 16;
        Description = "A massive overhead swing that splits the enemy in two, dealing enormous damage.";
        Type = SkillType.Active;
        AttackPower = 75;
        DefensePower = 0;
        SpeedModifier = 0;
        MagicPower = 0;
    }
}
