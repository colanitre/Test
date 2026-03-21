namespace RpgApi.Models;

public class Warrior : Class
{
    public Warrior() : base()
    {
        Name = "Warrior" ?? string.Empty;
        Archetype = "Warrior";
        Tier = 0;
        RequiredLevel = 0;
        IsAdvanced = false;
        BaseStrength = 15;
        BaseAgility = 10;
        BaseIntelligence = 5;
        BaseWisdom = 8;
        BaseCharisma = 7;
        BaseEndurance = 12;
        BaseLuck = 10;
        PhysicalResistance = 0.9;
        FireResistance = 1.0;
        IceResistance = 1.0;
        LightningResistance = 1.1;
        PoisonResistance = 0.85;
        HolyResistance = 1.0;
        ShadowResistance = 1.0;
        ArcaneResistance = 1.15;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new PowerStrike(),
            new Cleave(),
            new Whirlwind(),
            new Shockwave(),
            new IronSkin(),
            new LastStand(),
            new BattleCry()
        };
    }
}

