using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers;

[ApiController]
[Route("api/v1/runs")]
public class RogueliteRunsController : ControllerBase
{
    private readonly RpgContext _context;

    public RogueliteRunsController(RpgContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<RogueliteRunEntry>>> StartRun([FromBody] StartRunDto dto)
    {
        var characterExists = await _context.Characters.AnyAsync(c => c.Id == dto.CharacterId && c.PlayerId == dto.PlayerId);
        if (!characterExists)
            return NotFound(new ApiEnvelope<RogueliteRunEntry>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("character_not_found", "Character not found for player")));

        var enemies = await _context.Enemies
            .OrderBy(e => e.Level)
            .Select(e => new { e.Id, e.Level })
            .ToListAsync();

        if (enemies.Count < 2)
            return BadRequest(new ApiEnvelope<RogueliteRunEntry>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("not_enough_enemies", "Need at least 2 enemies to build a run")));

        var random = new Random(dto.Seed ?? Environment.TickCount);
        var length = Math.Clamp(dto.FightCount, 5, 10);
        var encounters = enemies.OrderBy(_ => random.Next()).Take(length - 1).Select(e => e.Id).ToList();
        var bossId = enemies.OrderByDescending(e => e.Level).First().Id;
        encounters.Add(bossId);

        var modifiers = new List<string>
        {
            "No Healing",
            "Fire Skills +15%",
            "Enemy Mana Regen +1",
            "Player Stamina Regen +1"
        }
        .OrderBy(_ => random.Next())
        .Take(2)
        .ToList();

        var runEntity = new RogueliteRun
        {
            Id = Guid.NewGuid(),
            PlayerId = dto.PlayerId,
            CharacterId = dto.CharacterId,
            CurrentFightIndex = 0,
            Completed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        runEntity.SetEnemyIds(encounters);
        runEntity.SetModifiers(modifiers);

        _context.RogueliteRuns.Add(runEntity);
        await _context.SaveChangesAsync();

        var run = new RogueliteRunEntry(
            runEntity.Id,
            runEntity.PlayerId,
            runEntity.CharacterId,
            runEntity.GetEnemyIds(),
            runEntity.CurrentFightIndex,
            runEntity.Completed,
            runEntity.CreatedAt,
            runEntity.UpdatedAt,
            runEntity.GetModifiers());

        return Ok(new ApiEnvelope<RogueliteRunEntry>(run, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiEnvelope<RogueliteRunEntry>>> GetRun(Guid id)
    {
        var runEntity = await _context.RogueliteRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (runEntity == null)
            return NotFound(new ApiEnvelope<RogueliteRunEntry>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("not_found", "Run not found")));

        var run = new RogueliteRunEntry(
            runEntity.Id,
            runEntity.PlayerId,
            runEntity.CharacterId,
            runEntity.GetEnemyIds(),
            runEntity.CurrentFightIndex,
            runEntity.Completed,
            runEntity.CreatedAt,
            runEntity.UpdatedAt,
            runEntity.GetModifiers());

        return Ok(new ApiEnvelope<RogueliteRunEntry>(run, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpPost("{id}/advance")]
    public async Task<ActionResult<ApiEnvelope<RogueliteRunEntry>>> AdvanceRun(Guid id)
    {
        var runEntity = await _context.RogueliteRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (runEntity == null)
            return NotFound(new ApiEnvelope<RogueliteRunEntry>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("not_found", "Run not found")));

        var enemyIds = runEntity.GetEnemyIds();
        var nextIndex = Math.Min(enemyIds.Count, runEntity.CurrentFightIndex + 1);
        runEntity.CurrentFightIndex = nextIndex;
        runEntity.Completed = nextIndex >= enemyIds.Count;
        runEntity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var updated = new RogueliteRunEntry(
            runEntity.Id,
            runEntity.PlayerId,
            runEntity.CharacterId,
            enemyIds,
            runEntity.CurrentFightIndex,
            runEntity.Completed,
            runEntity.CreatedAt,
            runEntity.UpdatedAt,
            runEntity.GetModifiers());

        return Ok(new ApiEnvelope<RogueliteRunEntry>(updated, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }
}

public record StartRunDto(int PlayerId, int CharacterId, int FightCount = 7, int? Seed = null);
