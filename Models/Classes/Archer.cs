namespace RpgApi.Models;

public class Archer : Class
{
    public Archer() : base()
    {
        Name = "Archer" ?? string.Empty;
        BaseStrength = 10;
        BaseAgility = 15;
        BaseIntelligence = 8;
        BaseWisdom = 12;
        BaseCharisma = 10;
        BaseEndurance = 8;
        BaseLuck = 12;
        Skills = new List<Skill>
        {
            new Archery(),
            new QuickShot()
        };
    }
}
