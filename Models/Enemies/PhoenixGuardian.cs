namespace RpgApi.Models;

public class PhoenixGuardian : EnemyClass
{
    public PhoenixGuardian() : base()
    {
        Name = "Phoenix Guardian";
        Description = "A mystical bird wreathed in flames, guardian of ancient secrets.";
        Type = EnemyType.Legendary;
        BaseLevel = 100;
        BaseStrength = 13;
        BaseAgility = 14;
        BaseIntelligence = 15;
        BaseWisdom = 16;
        BaseCharisma = 14;
        BaseEndurance = 16;
        BaseLuck = 12;
        Skills = new List<Skill> { new Fireball(), new HolyLight(), new Blizzard() };
    }
}
