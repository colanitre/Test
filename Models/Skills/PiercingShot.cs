namespace RpgApi.Models;

public class PiercingShot : Skill
{
    public PiercingShot() : base()
    {
        Name = "Piercing Shot";
        ManaCost = 0;
        StaminaCost = 10;
        Cooldown = 0;
        RequiredLevel = 1;
        RequiredAgility = 12;
        Description = "A precise arrow shot that pierces through armor.";
        Type = SkillType.Active;
        AttackPower = 40;
        DefensePower = 0;
        SpeedModifier = 4;
        MagicPower = 0;
    }
}
