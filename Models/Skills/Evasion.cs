namespace RpgApi.Models;

public class Evasion : Skill
{
    public Evasion() : base()
    {
        Name = "Evasion";
        ManaCost = 10;
        StaminaCost = 15;
        Cooldown = 2;
        RequiredLevel = 3;
        RequiredAgility = 14;
        Description = "Dodge incoming attacks and counterattack with increased agility.";
        Type = SkillType.Active;
        AttackPower = 20;
        DefensePower = 60;
        SpeedModifier = 8;
        MagicPower = 0;
    }
}
