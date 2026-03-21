namespace RpgApi.Models;

public class Archery : Skill
{
    public Archery() : base()
    {
        Name = "Archery" ?? string.Empty;
        ManaCost = 5;
        StaminaCost = 5;
        Cooldown = 3;
        RequiredLevel = 0;
        RequiredIntelligence = 10;
        RequiredWisdom = 10;
        Description = "A powerful archery skill." ?? string.Empty;
        Type = SkillType.Active;
        AttackPower = 35;
        DefensePower = 5;
        SpeedModifier = 25;
        MagicPower = 0;
    }
}
