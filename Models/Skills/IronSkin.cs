namespace RpgApi.Models;

public class IronSkin : Skill
{
    public IronSkin() : base()
    {
        Name = "Iron Skin";
        ManaCost = 8;
        StaminaCost = 12;
        Cooldown = 3;
        RequiredLevel = 2;
        RequiredEndurance = 15;
        RequiredStrength = 12;
        Description = "Harden your skin to reduce incoming damage.";
        Type = SkillType.Active;
        AttackPower = 10;
        DefensePower = 80;
        SpeedModifier = 0;
        MagicPower = 5;
    }
}
