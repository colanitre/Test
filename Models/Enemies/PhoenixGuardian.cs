namespace RpgApi.Models;

public class PhoenixGuardian : EnemyClass
{
    public PhoenixGuardian() : base()
    {
        Name = "Phoenix Guardian";
        Description = "A mystical bird wreathed in flames, guardian of ancient secrets.";
        Type = EnemyType.Legendary;
        BaseLevel = 100;
        BaseStrength = 13;
        BaseAgility = 14;
        BaseIntelligence = 15;
        BaseWisdom = 16;
        BaseCharisma = 14;
        BaseEndurance = 16;
        BaseLuck = 12;
        PhysicalResistance = 0.95;
        FireResistance = 0.55;
        IceResistance = 1.45;
        LightningResistance = 0.9;
        PoisonResistance = 0.7;
        HolyResistance = 0.75;
        ShadowResistance = 1.2;
        ArcaneResistance = 0.9;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Fireball(),
            new ArcaneBlast(),
            new Blizzard(),
            new HolyLight(),
            new Heal(),
            new MagicShield()
        };
    }
}
