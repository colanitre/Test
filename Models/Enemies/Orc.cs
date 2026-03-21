namespace RpgApi.Models;

public class Orc : EnemyClass
{
    public Orc() : base()
    {
        Name = "Orc";
        Description = "A brutish warrior with immense strength and low intelligence.";
        Type = EnemyType.Elite;
        BaseLevel = 3;
        BaseStrength = 15;
        BaseAgility = 8;
        BaseIntelligence = 4;
        BaseWisdom = 5;
        BaseCharisma = 3;
        BaseEndurance = 14;
        BaseLuck = 6;
        PhysicalResistance = 0.85;
        FireResistance = 1.0;
        IceResistance = 1.05;
        LightningResistance = 1.1;
        PoisonResistance = 0.8;
        HolyResistance = 1.0;
        ShadowResistance = 1.05;
        ArcaneResistance = 1.15;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new PowerStrike(),
            new Cleave(),
            new Shockwave(),
            new IronSkin(),
            new BattleCry()
        };
    }
}