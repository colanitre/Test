namespace RpgApi.Models;

public class ArcaneBlast : Skill
{
    public ArcaneBlast() : base()
    {
        Name = "Arcane Blast";
        ManaCost = 18;
        StaminaCost = 3;
        Cooldown = 1;
        RequiredLevel = 2;
        RequiredIntelligence = 13;
        Description = "Release a burst of pure magical energy at the enemy.";
        Type = SkillType.Active;
        AttackPower = 25;
        DefensePower = 0;
        SpeedModifier = 2;
        MagicPower = 50;
    }
}
