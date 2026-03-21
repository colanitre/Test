namespace RpgApi.Models;

public class ArcaneWard : Skill
{
    public ArcaneWard() : base()
    {
        Name = "Arcane Ward";
        Description = "A sustained magical ward that hardens the caster against incoming attacks.";
        Cooldown = 0;
        RequiredLevel = 3;
        RequiredIntelligence = 12;
        RequiredWisdom = 12;
        Type = SkillType.Passive;
        AttackPower = 0;
        DefensePower = 12;
        SpeedModifier = 0;
        MagicPower = 6;
        Element = ElementType.Arcane;
        ElementPowerMultiplier = 1.0;
    }
}
