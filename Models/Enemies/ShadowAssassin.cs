namespace RpgApi.Models;

public class ShadowAssassin : EnemyClass
{
    public ShadowAssassin() : base()
    {
        Name = "Shadow Assassin";
        Description = "A lethal killer that strikes from the shadows with precision.";
        Type = EnemyType.Elite;
        BaseLevel = 3;
        BaseStrength = 11;
        BaseAgility = 16;
        BaseIntelligence = 8;
        BaseWisdom = 7;
        BaseCharisma = 6;
        BaseEndurance = 9;
        BaseLuck = 10;
        PhysicalResistance = 0.95;
        FireResistance = 1.1;
        IceResistance = 1.0;
        LightningResistance = 0.9;
        PoisonResistance = 0.7;
        HolyResistance = 1.2;
        ShadowResistance = 0.6;
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
            new Windslash()
        };
    }
}
