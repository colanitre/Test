namespace RpgApi.Models;

public class Fireball : Skill
{
    public Fireball() : base()
    {
        Name = "Fireball" ?? string.Empty;
        ManaCost = 10;
        StaminaCost = 5;
        Cooldown = 3;
        RequiredLevel = 0;
        RequiredIntelligence = 10;
        RequiredWisdom = 10;
        Description = "A powerful fire-based spell." ?? string.Empty;
        Type = SkillType.Active;
        AttackPower = 20;
        DefensePower = 5;
        SpeedModifier = 3;
        MagicPower = 25;
    }
}
