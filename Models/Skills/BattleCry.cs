namespace RpgApi.Models;

public class BattleCry : Skill
{
    public BattleCry() : base()
    {
        Name = "Battle Cry";
        Description = "Rally your spirit to temporarily boost attack and defense.";
        Cooldown = 0;
        RequiredLevel = 0;
        RequiredStrength = 10;
        Type = SkillType.Passive;
        AttackPower = 5;
        DefensePower = 5;
        SpeedModifier = 0;
        MagicPower = 0;
    }
}