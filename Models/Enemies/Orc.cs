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
        Skills = new List<Skill> { new Fireball() };
    }
}