namespace RpgApi.Models;

public class Rogue : Class
{
    public Rogue() : base()
    {
        Name = "Rogue" ?? string.Empty;
        BaseStrength = 10;
        BaseAgility = 15;
        BaseIntelligence = 8;
        BaseWisdom = 12;
        BaseCharisma = 10;
        BaseEndurance = 8;
        BaseLuck = 15;
    }
}