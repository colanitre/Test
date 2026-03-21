namespace RpgApi.Models;

public class FightMove
{
    public int Id { get; set; }
    public Guid FightSessionId { get; set; }
    public virtual FightSession FightSession { get; set; } = null!;
    public int Turn { get; set; }
    public bool IsPlayer { get; set; }
    public int SkillId { get; set; }
    public int Damage { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
