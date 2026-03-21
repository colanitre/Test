namespace RpgApi.Models;

public class Class
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Archetype { get; set; } = string.Empty;
    public int Tier { get; set; } = 0;
    public int RequiredLevel { get; set; } = 0;
    public bool IsAdvanced { get; set; } = false;
    public int Branch { get; set; } = -1;

    // base stats
    public int BaseStrength { get; set; } = 10;
    public int BaseAgility { get; set; } = 10;
    public int BaseIntelligence { get; set; } = 10;
    public int BaseWisdom { get; set; } = 10;
    public int BaseCharisma { get; set; } = 10;
    public int BaseEndurance { get; set; } = 10;
    public int BaseLuck { get; set; } = 10;

    // Elemental resistance multipliers (lower is stronger resistance, higher is weaker)
    public double PhysicalResistance { get; set; } = 1.0;
    public double FireResistance { get; set; } = 1.0;
    public double IceResistance { get; set; } = 1.0;
    public double LightningResistance { get; set; } = 1.0;
    public double PoisonResistance { get; set; } = 1.0;
    public double HolyResistance { get; set; } = 1.0;
    public double ShadowResistance { get; set; } = 1.0;
    public double ArcaneResistance { get; set; } = 1.0;

    // Navigation property
    public ICollection<Character> Characters { get; set; } = [];
    public ICollection<Skill> Skills { get; set; } = [];
}