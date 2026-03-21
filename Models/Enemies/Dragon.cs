namespace RpgApi.Models;

public class Dragon : EnemyClass
{
    public Dragon() : base()
    {
        Name = "Ancient Dragon";
        Description = "A massive, fire-breathing beast with incredible power and wisdom.";
        Type = EnemyType.Legendary;
        BaseLevel = 100;
        BaseStrength = 20;
        BaseAgility = 15;
        BaseIntelligence = 18;
        BaseWisdom = 20;
        BaseCharisma = 12;
        BaseEndurance = 25;
        BaseLuck = 10;
        PhysicalResistance = 0.85;
        FireResistance = 0.6;
        IceResistance = 1.35;
        LightningResistance = 0.95;
        PoisonResistance = 0.8;
        HolyResistance = 1.0;
        ShadowResistance = 0.9;
        ArcaneResistance = 0.85;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Fireball(),
            new ArcaneBlast(),
            new ChainLightning(),
            new Blizzard(),
            new Meteor(),
            new MagicShield()
        };
    }
}