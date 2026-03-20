namespace RpgApi.Models;

public class Class
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // base stats
    public int BaseStrength { get; set; } = 10;
    public int BaseAgility { get; set; } = 10;
    public int BaseIntelligence { get; set; } = 10;
    public int BaseWisdom { get; set; } = 10;
    public int BaseCharisma { get; set; } = 10;
    public int BaseEndurance { get; set; } = 10;
    public int BaseLuck { get; set; } = 10;

    // Navigation property
    public ICollection<Character> Characters { get; set; } = [];
}