namespace RpgApi.Models;

public class EnemyClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EnemyType Type { get; set; } = EnemyType.Common;
    public int BaseLevel { get; set; } = 1;

    // base stats
    public int BaseStrength { get; set; } = 10;
    public int BaseAgility { get; set; } = 10;
    public int BaseIntelligence { get; set; } = 10;
    public int BaseWisdom { get; set; } = 10;
    public int BaseCharisma { get; set; } = 10;
    public int BaseEndurance { get; set; } = 10;
    public int BaseLuck { get; set; } = 10;

    // Navigation property
    public ICollection<Enemy> Enemies { get; set; } = [];
    public ICollection<Skill> Skills { get; set; } = [];
}