namespace RpgApi.Models;

public class Dragon : EnemyClass
{
    public Dragon() : base()
    {
        Name = "Ancient Dragon";
        Description = "A massive, fire-breathing beast with incredible power and wisdom.";
        Type = EnemyType.Legendary;
        BaseLevel = 100;
        BaseStrength = 20;
        BaseAgility = 15;
        BaseIntelligence = 18;
        BaseWisdom = 20;
        BaseCharisma = 12;
        BaseEndurance = 25;
        BaseLuck = 10;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Fireball(),
            new ArcaneBlast(),
            new ChainLightning(),
            new Blizzard(),
            new Meteor(),
            new MagicShield()
        };
    }
}