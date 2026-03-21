namespace RpgApi.Models;

public class Mage : Class
{
    public Mage() : base()
    {
        Name = "Mage" ?? string.Empty;
        Archetype = "Mage";
        Tier = 0;
        RequiredLevel = 0;
        IsAdvanced = false;
        BaseStrength = 5;
        BaseAgility = 7;
        BaseIntelligence = 15;
        BaseWisdom = 12;
        BaseCharisma = 10;
        BaseEndurance = 8;
        BaseLuck = 10;
        PhysicalResistance = 1.15;
        FireResistance = 0.95;
        IceResistance = 0.95;
        LightningResistance = 0.9;
        PoisonResistance = 1.1;
        HolyResistance = 0.95;
        ShadowResistance = 1.1;
        ArcaneResistance = 0.8;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new ArcaneSpark(),
            new ArcaneBlast(),
            new Fireball(),
            new IceShards(),
            new LightningBolt(),
            new ChainLightning(),
            new Blizzard(),
            new Meteor(),
            new MagicShield(),
            new TemporalShift(),
            new Heal(),
            new HolyLight(),
            new ArcaneWard(),
            new ManaBarrier()
        };
    }
}
