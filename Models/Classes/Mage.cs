namespace RpgApi.Models;

public class Mage : Class
{
    public Mage() : base()
    {
        Name = "Mage" ?? string.Empty;
        BaseStrength = 5;
        BaseAgility = 7;
        BaseIntelligence = 15;
        BaseWisdom = 12;
        BaseCharisma = 10;
        BaseEndurance = 8;
        BaseLuck = 10;
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
