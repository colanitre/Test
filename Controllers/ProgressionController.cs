using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers;

[ApiController]
[Route("api/v1/progression")]
public class ProgressionController : ControllerBase
{
    private readonly RpgContext _context;

    public ProgressionController(RpgContext context)
    {
        _context = context;
    }

    [HttpGet("talents/{className}")]
    public async Task<ActionResult<ApiEnvelope<IEnumerable<TalentNodeDto>>>> GetTalentTree(string className)
    {
        var key = className.Trim().ToLowerInvariant();
        var existing = await _context.TalentNodes
            .Where(t => t.ClassName == key)
            .OrderBy(t => t.Cost)
            .ToListAsync();

        if (existing.Count == 0)
        {
            var defaults = new List<TalentNode>
            {
                new() { Id = Guid.NewGuid(), ClassName = key, Name = "Arc I", Effect = "+1 mana regen", Category = "regen", Cost = 1, Unlocked = false },
                new() { Id = Guid.NewGuid(), ClassName = key, Name = "Arc II", Effect = "+5% skill power", Category = "skill", Cost = 1, Unlocked = false },
                new() { Id = Guid.NewGuid(), ClassName = key, Name = "Arc III", Effect = "-1 cooldown on basic active", Category = "cooldown", Cost = 2, Unlocked = false }
            };

            _context.TalentNodes.AddRange(defaults);
            await _context.SaveChangesAsync();
            existing = defaults;
        }

        var nodes = existing.Select(t => new TalentNodeDto(t.Id, t.Name, t.Effect, t.Category, t.Cost, t.Unlocked)).ToList();

        return Ok(new ApiEnvelope<IEnumerable<TalentNodeDto>>(nodes, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpPut("players/{playerId}/characters/{characterId}/equipment")]
    public async Task<ActionResult<ApiEnvelope<CharacterEquipmentDto>>> UpsertEquipment(int playerId, int characterId, [FromBody] CharacterEquipmentDto dto)
    {
        var exists = await _context.Characters.AnyAsync(c => c.Id == characterId && c.PlayerId == playerId);
        if (!exists)
            return NotFound(new ApiEnvelope<CharacterEquipmentDto>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("character_not_found", "Character not found")));

        var normalized = dto with
        {
            CharacterId = characterId,
            UpdatedAt = DateTime.UtcNow
        };

        var existing = await _context.CharacterEquipments.FirstOrDefaultAsync(e => e.CharacterId == characterId);
        if (existing == null)
        {
            existing = new CharacterEquipment { CharacterId = characterId };
            _context.CharacterEquipments.Add(existing);
        }

        existing.WeaponAffix = normalized.WeaponAffix;
        existing.ArmorAffix = normalized.ArmorAffix;
        existing.TrinketAffix = normalized.TrinketAffix;
        existing.ElementalResistMultiplier = normalized.ElementalResistMultiplier;
        existing.CooldownModifier = normalized.CooldownModifier;
        existing.UpdatedAt = normalized.UpdatedAt;
        await _context.SaveChangesAsync();

        return Ok(new ApiEnvelope<CharacterEquipmentDto>(normalized, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpGet("players/{playerId}/characters/{characterId}/equipment")]
    public async Task<ActionResult<ApiEnvelope<CharacterEquipmentDto>>> GetEquipment(int playerId, int characterId)
    {
        var exists = await _context.Characters.AnyAsync(c => c.Id == characterId && c.PlayerId == playerId);
        if (!exists)
            return NotFound(new ApiEnvelope<CharacterEquipmentDto>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("character_not_found", "Character not found")));

        var entity = await _context.CharacterEquipments.FirstOrDefaultAsync(e => e.CharacterId == characterId);
        if (entity == null)
        {
            entity = new CharacterEquipment
            {
                CharacterId = characterId,
                ElementalResistMultiplier = 1.0,
                CooldownModifier = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.CharacterEquipments.Add(entity);
            await _context.SaveChangesAsync();
        }

        var value = new CharacterEquipmentDto(
            entity.CharacterId,
            entity.WeaponAffix,
            entity.ArmorAffix,
            entity.TrinketAffix,
            entity.ElementalResistMultiplier,
            entity.CooldownModifier,
            entity.UpdatedAt);
        return Ok(new ApiEnvelope<CharacterEquipmentDto>(value, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpGet("challenges")]
    public ActionResult<ApiEnvelope<object>> GetChallenges()
    {
        var now = DateTime.UtcNow;
        var data = new
        {
            daily = new[]
            {
                new { id = "daily-no-heal", title = "No Healing", constraint = "No healing skills", reward = 200 },
                new { id = "daily-fire-only", title = "Fire Specialist", constraint = "Fire skills only", reward = 180 }
            },
            weekly = new[]
            {
                new { id = "weekly-win-streak", title = "Win Streak", constraint = "Win 10 fights in a row", reward = 1200 },
                new { id = "weekly-speed-clear", title = "Speed Clear", constraint = "Clear 5 fights under 30 turns", reward = 1000 }
            },
            generatedAt = now
        };

        return Ok(new ApiEnvelope<object>(data, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpGet("ladders/seasonal")]
    public ActionResult<ApiEnvelope<object>> GetSeasonalLadder()
    {
        var data = new
        {
            season = "Season 1",
            metrics = new[] { "fastest_clear", "highest_wave", "best_win_streak" },
            leaderboard = new[]
            {
                new { rank = 1, player = "Skyblade", fastest_clear = 88, highest_wave = 17, best_win_streak = 22 },
                new { rank = 2, player = "ArcNova", fastest_clear = 91, highest_wave = 16, best_win_streak = 20 },
                new { rank = 3, player = "IronMist", fastest_clear = 93, highest_wave = 15, best_win_streak = 18 }
            }
        };

        return Ok(new ApiEnvelope<object>(data, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }
}

public record TalentNodeDto(Guid Id, string Name, string Effect, string Category, int Cost, bool Unlocked);

public record CharacterEquipmentDto(
    int CharacterId,
    string? WeaponAffix,
    string? ArmorAffix,
    string? TrinketAffix,
    double ElementalResistMultiplier,
    int CooldownModifier,
    DateTime UpdatedAt);
