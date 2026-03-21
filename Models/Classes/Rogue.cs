namespace RpgApi.Models;

public class Rogue : Class
{
    public Rogue() : base()
    {
        Name = "Rogue" ?? string.Empty;
        Archetype = "Rogue";
        Tier = 0;
        RequiredLevel = 0;
        IsAdvanced = false;
        BaseStrength = 10;
        BaseAgility = 15;
        BaseIntelligence = 8;
        BaseWisdom = 12;
        BaseCharisma = 10;
        BaseEndurance = 8;
        BaseLuck = 15;
        PhysicalResistance = 1.0;
        FireResistance = 1.1;
        IceResistance = 1.0;
        LightningResistance = 0.9;
        PoisonResistance = 0.8;
        HolyResistance = 1.05;
        ShadowResistance = 0.85;
        ArcaneResistance = 1.0;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new ShadowStab(),
            new Backstab(),
            new DeadlyPoison(),
            new VenomStrike(),
            new Evasion(),
            new RippleStrike()
        };
    }
}