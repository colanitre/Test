namespace RpgApi.Models;

public class Troll : EnemyClass
{
    public Troll() : base()
    {
        Name = "Troll";
        Description = "A massive creature with regenerating flesh, extremely hard to kill.";
        Type = EnemyType.Elite;
        BaseLevel = 4;
        BaseStrength = 16;
        BaseAgility = 7;
        BaseIntelligence = 4;
        BaseWisdom = 6;
        BaseCharisma = 5;
        BaseEndurance = 18;
        BaseLuck = 5;
        Skills = new List<Skill> { new PowerStrike(), new IronSkin() };
    }
}
