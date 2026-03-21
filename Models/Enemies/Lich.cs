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
