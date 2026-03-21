namespace RpgApi.Models;

public class TemporalShift : Skill
{
    public TemporalShift() : base()
    {
        Name = "Temporal Shift";
        ManaCost = 35;
        StaminaCost = 10;
        Cooldown = 6;
        RequiredLevel = 8;
        RequiredIntelligence = 16;
        RequiredWisdom = 15;
        Description = "Bend time itself to slow enemies or speed allies, affecting the flow of battle.";
        Type = SkillType.Active;
        AttackPower = 10;
        DefensePower = 30;
        SpeedModifier = 10;
        MagicPower = 70;
        Element = ElementType.Arcane;
        ElementPowerMultiplier = 1.2;
    }
}
