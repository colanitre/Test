namespace RpgApi.Models;

public class OlympianRadiantOracle : EnemyClass
{
    public OlympianRadiantOracle() : base()
    {
        Name = "Olympian Radiant Oracle";
        Description = "A divine seer channeling sunfire, prophecy, and healing light.";
        Type = EnemyType.Godlike;
        BaseLevel = 1700;
        BaseStrength = 22;
        BaseAgility = 27;
        BaseIntelligence = 38;
        BaseWisdom = 39;
        BaseCharisma = 30;
        BaseEndurance = 26;
        BaseLuck = 25;
        PhysicalResistance = 0.95;
        FireResistance = 0.72;
        IceResistance = 1.05;
        LightningResistance = 0.8;
        PoisonResistance = 0.88;
        HolyResistance = 0.5;
        ShadowResistance = 1.2;
        ArcaneResistance = 0.62;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new HolyLight(),
            new Heal(),
            new ArcaneSpark(),
            new Meteor(),
            new MagicShield()
        };
    }
}
