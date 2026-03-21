namespace RpgApi.Models;

public class Backstab : Skill
{
    public Backstab() : base()
    {
        Name = "Backstab";
        ManaCost = 0;
        StaminaCost = 12;
        Cooldown = 1;
        RequiredLevel = 3;
        RequiredAgility = 15;
        RequiredStrength = 10;
        Description = "Strike from the shadows, dealing critical damage to an unaware enemy.";
        Type = SkillType.Active;
        AttackPower = 80;
        DefensePower = 0;
        SpeedModifier = 6;
        MagicPower = 0;
        Element = ElementType.Physical;
        ElementPowerMultiplier = 1.05;
    }
}
