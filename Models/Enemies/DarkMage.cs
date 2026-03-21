namespace RpgApi.Models;

public class DarkMage : EnemyClass
{
    public DarkMage() : base()
    {
        Name = "Dark Mage";
        Description = "A twisted mage wielding forbidden dark magic.";
        Type = EnemyType.Elite;
        BaseLevel = 3;
        BaseStrength = 6;
        BaseAgility = 9;
        BaseIntelligence = 16;
        BaseWisdom = 14;
        BaseCharisma = 10;
        BaseEndurance = 8;
        BaseLuck = 7;
        PhysicalResistance = 1.1;
        FireResistance = 0.95;
        IceResistance = 0.95;
        LightningResistance = 0.9;
        PoisonResistance = 1.05;
        HolyResistance = 1.2;
        ShadowResistance = 0.7;
        ArcaneResistance = 0.8;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new ArcaneBlast(),
            new Fireball(),
            new IceShards(),
            new LightningBolt(),
            new ArcaneWard()
        };
    }
}
