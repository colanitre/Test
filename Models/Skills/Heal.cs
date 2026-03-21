namespace RpgApi.Models;

public class Heal : Skill
{
    public Heal() : base()
    {
        Name = "Heal";
        ManaCost = 25;
        StaminaCost = 0;
        Cooldown = 3;
        RequiredLevel = 5;
        RequiredWisdom = 14;
        RequiredCharisma = 10;
        Description = "Restore health to yourself through magical intervention.";
        Type = SkillType.Active;
        AttackPower = 0;
        DefensePower = 20;
        SpeedModifier = 0;
        MagicPower = 50;
        Element = ElementType.Holy;
        ElementPowerMultiplier = 1.0;
    }
}
