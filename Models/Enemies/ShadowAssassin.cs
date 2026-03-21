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
        Skills = new List<Skill> { new DeadlyPoison(), new Windslash() };
    }
}
