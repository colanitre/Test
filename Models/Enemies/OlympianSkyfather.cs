namespace RpgApi.Models;

public class OlympianSkyfather : EnemyClass
{
    public OlympianSkyfather() : base()
    {
        Name = "Olympian Skyfather";
        Description = "A thunder sovereign who commands storms, light, and the will of Olympus.";
        Type = EnemyType.Godlike;
        BaseLevel = 1100;
        BaseStrength = 30;
        BaseAgility = 26;
        BaseIntelligence = 35;
        BaseWisdom = 34;
        BaseCharisma = 28;
        BaseEndurance = 32;
        BaseLuck = 24;
        PhysicalResistance = 0.78;
        FireResistance = 0.85;
        IceResistance = 1.1;
        LightningResistance = 0.45;
        PoisonResistance = 0.9;
        HolyResistance = 0.7;
        ShadowResistance = 1.0;
        ArcaneResistance = 0.75;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new ChainLightning(),
            new LightningBolt(),
            new ArcaneBlast(),
            new MagicShield(),
            new TemporalShift()
        };
    }
}
