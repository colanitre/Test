using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers;

[ApiController]
[Route("api/v1/admin/balance")]
public class AdminBalanceController : ControllerBase
{
    private readonly RpgContext _context;
    private readonly IConfiguration _configuration;

    public AdminBalanceController(RpgContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("skills")]
    public async Task<ActionResult<ApiEnvelope<IEnumerable<AdminSkillBalanceDto>>>> GetSkills()
    {
        if (!IsAuthorized())
            return Unauthorized(new ApiEnvelope<IEnumerable<AdminSkillBalanceDto>>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("unauthorized", "Missing or invalid admin key")));

        var items = await _context.Skills
            .OrderBy(s => s.Name)
            .Select(s => new AdminSkillBalanceDto(s.Id, s.Name, s.Type, s.RequiredLevel, s.ManaCost, s.StaminaCost, s.Cooldown, s.AttackPower, s.DefensePower, s.SpeedModifier, s.MagicPower, s.ElementPowerMultiplier))
            .ToListAsync();

        return Ok(new ApiEnvelope<IEnumerable<AdminSkillBalanceDto>>(items, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpPut("skills/{id}")]
    public async Task<ActionResult<ApiEnvelope<AdminSkillBalanceDto>>> UpdateSkill(int id, [FromBody] UpdateSkillBalanceDto dto)
    {
        if (!IsAuthorized())
            return Unauthorized(new ApiEnvelope<AdminSkillBalanceDto>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("unauthorized", "Missing or invalid admin key")));

        var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == id);
        if (skill == null)
            return NotFound(new ApiEnvelope<AdminSkillBalanceDto>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("not_found", "Skill not found")));

        skill.ManaCost = dto.ManaCost;
        skill.StaminaCost = dto.StaminaCost;
        skill.Cooldown = dto.Cooldown;
        skill.AttackPower = dto.AttackPower;
        skill.DefensePower = dto.DefensePower;
        skill.SpeedModifier = dto.SpeedModifier;
        skill.MagicPower = dto.MagicPower;
        skill.ElementPowerMultiplier = dto.ElementPowerMultiplier;

        await _context.SaveChangesAsync();

        var result = new AdminSkillBalanceDto(skill.Id, skill.Name, skill.Type, skill.RequiredLevel, skill.ManaCost, skill.StaminaCost, skill.Cooldown, skill.AttackPower, skill.DefensePower, skill.SpeedModifier, skill.MagicPower, skill.ElementPowerMultiplier);
        return Ok(new ApiEnvelope<AdminSkillBalanceDto>(result, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpPost("recalculate")]
    public ActionResult<ApiEnvelope<object>> RecalculateBalance()
    {
        if (!IsAuthorized())
            return Unauthorized(new ApiEnvelope<object>(default, new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier), new ApiError("unauthorized", "Missing or invalid admin key")));

        return Ok(new ApiEnvelope<object>(
            new { status = "queued", message = "Balance recalculation trigger accepted" },
            new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    private bool IsAuthorized()
    {
        var expected = _configuration["Admin:BalanceApiKey"];
        if (string.IsNullOrWhiteSpace(expected))
            expected = "dev-admin-key";

        var actual = Request.Headers["X-Admin-Key"].FirstOrDefault();
        return string.Equals(expected, actual, StringComparison.Ordinal);
    }
}

public record AdminSkillBalanceDto(
    int Id,
    string Name,
    SkillType Type,
    int RequiredLevel,
    int ManaCost,
    int StaminaCost,
    int Cooldown,
    int AttackPower,
    int DefensePower,
    int SpeedModifier,
    int MagicPower,
    double ElementPowerMultiplier);

public record UpdateSkillBalanceDto(
    int ManaCost,
    int StaminaCost,
    int Cooldown,
    int AttackPower,
    int DefensePower,
    int SpeedModifier,
    int MagicPower,
    double ElementPowerMultiplier);
