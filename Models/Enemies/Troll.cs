namespace RpgApi.Models;

public class Troll : EnemyClass
{
    public Troll() : base()
    {
        Name = "Troll";
        Description = "A massive creature with regenerating flesh, extremely hard to kill.";
        Type = EnemyType.Elite;
        BaseLevel = 4;
        BaseStrength = 16;
        BaseAgility = 7;
        BaseIntelligence = 4;
        BaseWisdom = 6;
        BaseCharisma = 5;
        BaseEndurance = 18;
        BaseLuck = 5;
        PhysicalResistance = 0.8;
        FireResistance = 1.25;
        IceResistance = 0.85;
        LightningResistance = 1.0;
        PoisonResistance = 0.85;
        HolyResistance = 1.0;
        ShadowResistance = 1.0;
        ArcaneResistance = 1.1;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new PowerStrike(),
            new Shockwave(),
            new IronSkin(),
            new LastStand()
        };
    }
}
