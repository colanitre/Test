using System.Collections.Concurrent;

namespace RpgApi.Models;

public enum BattleRow
{
    Front,
    Mid,
    Back
}

public enum StatusEffectType
{
    Burn,
    Poison,
    Freeze,
    Stun,
    Bleed,
    ShieldBreak
}

public record StatusEffectState(StatusEffectType Type, int RemainingTurns, int Potency = 1, int? SourceSkillId = null);

public record ProgressionEventEntry(DateTime Timestamp, string Type, string Message, int? PlayerId = null, int? CharacterId = null, string? Metadata = null);

public record CharacterLoadoutEntry(Guid Id, string Name, List<int> ActiveSkillOrder, List<int> PassiveSkillIds, DateTime UpdatedAt);

public record RogueliteRunEntry(
    Guid Id,
    int PlayerId,
    int CharacterId,
    List<int> EnemyIds,
    int CurrentFightIndex,
    bool Completed,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<string> Modifiers);

public static class RuntimeStores
{
    public static readonly ConcurrentDictionary<string, object> IdempotencyResponses = new();

    public static readonly ConcurrentDictionary<Guid, List<StatusEffectState>> CharacterStatusEffects = new();
    public static readonly ConcurrentDictionary<Guid, List<StatusEffectState>> EnemyStatusEffects = new();

    public static readonly ConcurrentDictionary<Guid, BattleRow> CharacterRows = new();
    public static readonly ConcurrentDictionary<Guid, BattleRow> EnemyRows = new();

    public static readonly ConcurrentDictionary<string, List<CharacterLoadoutEntry>> CharacterLoadouts = new();

    public static readonly ConcurrentQueue<ProgressionEventEntry> ProgressionEvents = new();

    public static readonly ConcurrentDictionary<Guid, RogueliteRunEntry> RogueliteRuns = new();
}
