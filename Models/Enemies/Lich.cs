namespace RpgApi.Models;

public class Lich : EnemyClass
{
    public Lich() : base()
    {
        Name = "Lich";
        Description = "An ancient undead sorcerer with mastery over death magic.";
        Type = EnemyType.Boss;
        BaseLevel = 100;
        BaseStrength = 8;
        BaseAgility = 10;
        BaseIntelligence = 18;
        BaseWisdom = 17;
        BaseCharisma = 12;
        BaseEndurance = 11;
        BaseLuck = 8;
        PhysicalResistance = 0.95;
        FireResistance = 1.15;
        IceResistance = 0.8;
        LightningResistance = 0.95;
        PoisonResistance = 0.0;
        HolyResistance = 1.35;
        ShadowResistance = 0.5;
        ArcaneResistance = 0.75;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Fireball(),
            new ArcaneBlast(),
            new Blizzard(),
            new ChainLightning(),
            new Meteor(),
            new HolyLight(),
            new TemporalShift(),
            new MagicShield(),
            new ArcaneWard(),
            new ManaBarrier()
        };
    }
}
