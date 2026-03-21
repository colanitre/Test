namespace RpgApi.Models;

public class OlympianSeaLord : EnemyClass
{
    public OlympianSeaLord() : base()
    {
        Name = "Olympian Sea Lord";
        Description = "A tidal deity whose fury surges like abyssal currents.";
        Type = EnemyType.Godlike;
        BaseLevel = 1200;
        BaseStrength = 34;
        BaseAgility = 24;
        BaseIntelligence = 32;
        BaseWisdom = 33;
        BaseCharisma = 22;
        BaseEndurance = 36;
        BaseLuck = 20;
        PhysicalResistance = 0.72;
        FireResistance = 1.2;
        IceResistance = 0.75;
        LightningResistance = 1.05;
        PoisonResistance = 0.85;
        HolyResistance = 0.8;
        ShadowResistance = 0.95;
        ArcaneResistance = 0.8;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Blizzard(),
            new IceShards(),
            new RippleStrike(),
            new MagicShield(),
            new Heal()
        };
    }
}
