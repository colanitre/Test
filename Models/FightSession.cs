using System.Text.Json;

namespace RpgApi.Models;

public enum FightStatus
{
    Waiting,
    InProgress,
    Completed,
    Stale
}

public class FightSession
{
    public Guid Id { get; set; }
    public int PlayerId { get; set; }
    public int CharacterId { get; set; }
    public int EnemyId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string EnemyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActionAt { get; set; } = DateTime.UtcNow;
    public FightStatus Status { get; set; } = FightStatus.InProgress;
    public bool IsLocked { get; set; } = false;
    public int LockedByPlayerId { get; set; }
    public int CharacterCurrentHp { get; set; }
    public int EnemyCurrentHp { get; set; }
    public int CharacterMaxHp { get; set; }
    public int EnemyMaxHp { get; set; }
    public int CharacterCurrentMana { get; set; }
    public int CharacterMaxMana { get; set; }
    public int EnemyCurrentMana { get; set; }
    public int EnemyMaxMana { get; set; }
    public int CharacterCurrentStamina { get; set; }
    public int CharacterMaxStamina { get; set; }
    public int EnemyCurrentStamina { get; set; }
    public int EnemyMaxStamina { get; set; }
    public int CharacterHealthRegen { get; set; }
    public int CharacterManaRegen { get; set; }
    public int CharacterStaminaRegen { get; set; }
    public int EnemyHealthRegen { get; set; }
    public int EnemyManaRegen { get; set; }
    public int EnemyStaminaRegen { get; set; }
    public int CharacterAttack { get; set; }
    public int CharacterDefense { get; set; }
    public int EnemyAttack { get; set; }
    public int EnemyDefense { get; set; }
    public int CharacterLevel { get; set; }
    public int EnemyLevel { get; set; }
    public bool IsVictory { get; set; }
    public int CurrentTurn { get; set; } = 1;

    // JSON stored state for cooldowns and skill lists
    public string CharacterCooldownJson { get; set; } = JsonSerializer.Serialize(new Dictionary<int, int>());
    public string EnemyCooldownJson { get; set; } = JsonSerializer.Serialize(new Dictionary<int, int>());
    public string CharacterSkillIdsJson { get; set; } = JsonSerializer.Serialize(new List<int>());
    public string EnemySkillIdsJson { get; set; } = JsonSerializer.Serialize(new List<int>());
    
    // Passive skills tracking
    public string CharacterActivePassiveSkillsJson { get; set; } = JsonSerializer.Serialize(new List<int>());
    public string EnemyActivePassiveSkillsJson { get; set; } = JsonSerializer.Serialize(new List<int>());

    public virtual ICollection<FightMove> Moves { get; set; } = new List<FightMove>();

    public Dictionary<int, int> GetCharacterCooldowns() => JsonSerializer.Deserialize<Dictionary<int, int>>(CharacterCooldownJson) ?? new();
    public Dictionary<int, int> GetEnemyCooldowns() => JsonSerializer.Deserialize<Dictionary<int, int>>(EnemyCooldownJson) ?? new();
    public List<int> GetCharacterSkillIds() => JsonSerializer.Deserialize<List<int>>(CharacterSkillIdsJson) ?? new();
    public List<int> GetEnemySkillIds() => JsonSerializer.Deserialize<List<int>>(EnemySkillIdsJson) ?? new();

    public void SetCharacterCooldowns(Dictionary<int, int> values) => CharacterCooldownJson = JsonSerializer.Serialize(values);
    public void SetEnemyCooldowns(Dictionary<int, int> values) => EnemyCooldownJson = JsonSerializer.Serialize(values);
    public void SetCharacterSkillIds(List<int> ids) => CharacterSkillIdsJson = JsonSerializer.Serialize(ids);
    public void SetEnemySkillIds(List<int> ids) => EnemySkillIdsJson = JsonSerializer.Serialize(ids);
    
    public List<int> GetCharacterActivePassiveSkills() => JsonSerializer.Deserialize<List<int>>(CharacterActivePassiveSkillsJson) ?? new();
    public List<int> GetEnemyActivePassiveSkills() => JsonSerializer.Deserialize<List<int>>(EnemyActivePassiveSkillsJson) ?? new();
    
    public void SetCharacterActivePassiveSkills(List<int> ids) => CharacterActivePassiveSkillsJson = JsonSerializer.Serialize(ids);
    public void SetEnemyActivePassiveSkills(List<int> ids) => EnemyActivePassiveSkillsJson = JsonSerializer.Serialize(ids);
}
