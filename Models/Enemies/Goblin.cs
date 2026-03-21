namespace RpgApi.Models;

public class Goblin : EnemyClass
{
    public Goblin() : base()
    {
        Name = "Goblin";
        Description = "A small, cunning creature that attacks in groups.";
        Type = EnemyType.Common;
        BaseLevel = 1;
        BaseStrength = 8;
        BaseAgility = 12;
        BaseIntelligence = 6;
        BaseWisdom = 5;
        BaseCharisma = 4;
        BaseEndurance = 7;
        BaseLuck = 8;
        Skills = new List<Skill>
        {
            new StarterStrike(),
            new StarterGuard(),
            new Archery(),
            new QuickShot(),
            new VenomStrike()
        };
    }
}