namespace RpgApi.Models;

public class VenomStrike : Skill
{
    public VenomStrike() : base()
    {
        Name = "Venom Strike";
        ManaCost = 8;
        StaminaCost = 14;
        Cooldown = 2;
        RequiredLevel = 4;
        RequiredAgility = 13;
        RequiredIntelligence = 12;
        Description = "Inject venom into a quick strike, poisoning the enemy over time.";
        Type = SkillType.Active;
        AttackPower = 45;
        DefensePower = 0;
        SpeedModifier = 5;
        MagicPower = 25;
    }
}
