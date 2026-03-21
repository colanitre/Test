namespace RpgApi.Models;

public class Warrior : Class
{
    public Warrior() : base()
    {
        Name = "Warrior" ?? string.Empty;
        BaseStrength = 15;
        BaseAgility = 10;
        BaseIntelligence = 5;
        BaseWisdom = 8;
        BaseCharisma = 7;
        BaseEndurance = 12;
        BaseLuck = 10;
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

