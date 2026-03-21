namespace RpgApi.Models;

public class Goblin : EnemyClass
{
    public Goblin() : base()
    {
        Name = "Goblin";
        Description = "A small, cunning creature that attacks in groups.";
        Type = EnemyType.Common;
        BaseLevel = 1;
        BaseStrength = 8;
        BaseAgility = 12;
        BaseIntelligence = 6;
        BaseWisdom = 5;
        BaseCharisma = 4;
        BaseEndurance = 7;
        BaseLuck = 8;
        PhysicalResistance = 1.0;
        FireResistance = 1.2;
        IceResistance = 0.9;
        LightningResistance = 1.0;
        PoisonResistance = 0.9;
        HolyResistance = 1.0;
        ShadowResistance = 0.95;
        ArcaneResistance = 1.1;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Archery(),
            new QuickShot(),
            new VenomStrike()
        };
    }
}