using System.Text.Json;

namespace RpgApi.Models;

public class CharacterLoadout
{
    public Guid Id { get; set; }
    public int PlayerId { get; set; }
    public int CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ActiveSkillOrderJson { get; set; } = JsonSerializer.Serialize(new List<int>());
    public string PassiveSkillIdsJson { get; set; } = JsonSerializer.Serialize(new List<int>());
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<int> GetActiveSkillOrder() => JsonSerializer.Deserialize<List<int>>(ActiveSkillOrderJson) ?? [];
    public List<int> GetPassiveSkillIds() => JsonSerializer.Deserialize<List<int>>(PassiveSkillIdsJson) ?? [];
    public void SetActiveSkillOrder(List<int> ids) => ActiveSkillOrderJson = JsonSerializer.Serialize(ids);
    public void SetPassiveSkillIds(List<int> ids) => PassiveSkillIdsJson = JsonSerializer.Serialize(ids);
}

public class ProgressionEvent
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? PlayerId { get; set; }
    public int? CharacterId { get; set; }
    public string? Metadata { get; set; }
}

public class RogueliteRun
{
    public Guid Id { get; set; }
    public int PlayerId { get; set; }
    public int CharacterId { get; set; }
    public string EnemyIdsJson { get; set; } = JsonSerializer.Serialize(new List<int>());
    public int CurrentFightIndex { get; set; }
    public bool Completed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string ModifiersJson { get; set; } = JsonSerializer.Serialize(new List<string>());

    public List<int> GetEnemyIds() => JsonSerializer.Deserialize<List<int>>(EnemyIdsJson) ?? [];
    public List<string> GetModifiers() => JsonSerializer.Deserialize<List<string>>(ModifiersJson) ?? [];
    public void SetEnemyIds(List<int> ids) => EnemyIdsJson = JsonSerializer.Serialize(ids);
    public void SetModifiers(List<string> modifiers) => ModifiersJson = JsonSerializer.Serialize(modifiers);
}

public class TalentNode
{
    public Guid Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Effect { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Cost { get; set; }
    public bool Unlocked { get; set; }
}

public class CharacterEquipment
{
    public int CharacterId { get; set; }
    public string? WeaponAffix { get; set; }
    public string? ArmorAffix { get; set; }
    public string? TrinketAffix { get; set; }
    public double ElementalResistMultiplier { get; set; } = 1.0;
    public int CooldownModifier { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
