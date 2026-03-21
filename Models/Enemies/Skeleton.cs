namespace RpgApi.Models;

public class Skeleton : EnemyClass
{
    public Skeleton() : base()
    {
        Name = "Skeleton";
        Description = "An undead warrior rising from the grave, bones rattling with dark energy.";
        Type = EnemyType.Common;
        BaseLevel = 2;
        BaseStrength = 10;
        BaseAgility = 8;
        BaseIntelligence = 5;
        BaseWisdom = 4;
        BaseCharisma = 3;
        BaseEndurance = 9;
        BaseLuck = 6;
        PhysicalResistance = 0.9;
        FireResistance = 1.25;
        IceResistance = 0.75;
        LightningResistance = 1.0;
        PoisonResistance = 0.0;
        HolyResistance = 1.3;
        ShadowResistance = 0.7;
        ArcaneResistance = 1.0;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new PowerStrike(),
            new Cleave()
        };
    }
}
