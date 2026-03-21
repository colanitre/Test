namespace RpgApi.Models;

public class Archer : Class
{
    public Archer() : base()
    {
        Name = "Archer" ?? string.Empty;
        Archetype = "Archer";
        Tier = 0;
        RequiredLevel = 0;
        IsAdvanced = false;
        BaseStrength = 10;
        BaseAgility = 15;
        BaseIntelligence = 8;
        BaseWisdom = 12;
        BaseCharisma = 10;
        BaseEndurance = 8;
        BaseLuck = 12;
        PhysicalResistance = 1.0;
        FireResistance = 1.15;
        IceResistance = 0.9;
        LightningResistance = 1.0;
        PoisonResistance = 0.95;
        HolyResistance = 1.0;
        ShadowResistance = 1.05;
        ArcaneResistance = 1.0;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Archery(),
            new QuickShot(),
            new PiercingShot(),
            new MultiShot(),
            new Volley(),
            new Windslash()
        };
    }
}
