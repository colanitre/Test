namespace RpgApi.Models;

public class OlympianWarbringer : EnemyClass
{
    public OlympianWarbringer() : base()
    {
        Name = "Olympian Warbringer";
        Description = "A relentless god of battle clad in blood-forged armor.";
        Type = EnemyType.Godlike;
        BaseLevel = 1500;
        BaseStrength = 40;
        BaseAgility = 28;
        BaseIntelligence = 20;
        BaseWisdom = 22;
        BaseCharisma = 20;
        BaseEndurance = 38;
        BaseLuck = 18;
        PhysicalResistance = 0.58;
        FireResistance = 0.85;
        IceResistance = 0.95;
        LightningResistance = 0.9;
        PoisonResistance = 0.75;
        HolyResistance = 1.0;
        ShadowResistance = 0.9;
        ArcaneResistance = 1.1;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new PowerStrike(),
            new Cleave(),
            new Whirlwind(),
            new Shockwave(),
            new LastStand()
        };
    }
}
