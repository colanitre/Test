namespace RpgApi.Models;

public class OlympianUnderworldKing : EnemyClass
{
    public OlympianUnderworldKing() : base()
    {
        Name = "Olympian Underworld King";
        Description = "A ruler of souls and shadow, feared by mortals and monsters alike.";
        Type = EnemyType.Godlike;
        BaseLevel = 1300;
        BaseStrength = 29;
        BaseAgility = 25;
        BaseIntelligence = 37;
        BaseWisdom = 35;
        BaseCharisma = 26;
        BaseEndurance = 31;
        BaseLuck = 23;
        PhysicalResistance = 0.88;
        FireResistance = 0.8;
        IceResistance = 0.85;
        LightningResistance = 0.9;
        PoisonResistance = 0.7;
        HolyResistance = 1.25;
        ShadowResistance = 0.45;
        ArcaneResistance = 0.72;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new ShadowStab(),
            new VenomStrike(),
            new ArcaneBlast(),
            new ArcaneWard(),
            new ManaBarrier()
        };
    }
}
