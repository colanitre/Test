namespace RpgApi.Models;

public class ManaBarrier : Skill
{
    public ManaBarrier() : base()
    {
        Name = "Mana Barrier";
        Description = "Convert ambient mana into a constant defensive barrier that reduces damage taken.";
        Cooldown = 0;
        RequiredLevel = 6;
        RequiredIntelligence = 15;
        RequiredWisdom = 14;
        Type = SkillType.Passive;
        AttackPower = 0;
        DefensePower = 20;
        SpeedModifier = -1;
        MagicPower = 10;
        Element = ElementType.Arcane;
        ElementPowerMultiplier = 1.0;
    }
}
