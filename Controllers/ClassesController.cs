using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace RpgApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly RpgContext _context;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(RpgContext context, ILogger<ClassesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all classes, including tier progression metadata.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassAdminDto>>> GetClasses()
    {
        _logger.LogInformation("Fetching all classes for admin");

        var classes = await _context.Classes
            .Include(c => c.Skills)
            .ToListAsync();

        var result = classes
            .OrderBy(c => c.Archetype)
            .ThenBy(c => c.Tier)
            .ThenBy(c => c.Branch)
            .ThenBy(c => c.Name)
            .Select(c => new ClassAdminDto(
                c.Id,
                c.Name,
                c.Archetype,
                c.Tier,
                c.RequiredLevel,
                c.IsAdvanced,
                c.Branch,
                c.BaseStrength,
                c.BaseAgility,
                c.BaseIntelligence,
                c.BaseWisdom,
                c.BaseCharisma,
                c.BaseEndurance,
                c.BaseLuck,
                c.Skills.Count));

        return Ok(result);
    }

    [HttpGet("/api/v1/classes")]
    public async Task<ActionResult<ApiEnvelope<object>>> GetClassesV1()
    {
        var classes = await _context.Classes
            .Include(c => c.Skills)
            .OrderBy(c => c.Archetype)
            .ThenBy(c => c.Tier)
            .ThenBy(c => c.Branch)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var tagSource = string.Join("|", classes.Select(c => $"{c.Id}:{c.Name}:{c.Tier}:{c.RequiredLevel}:{c.Skills.Count}"));
        var etag = ComputeWeakETag(tagSource);
        if (Request.Headers.IfNoneMatch.Any(h => h == etag))
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;

        var result = classes.Select(c => new ClassAdminDto(
            c.Id,
            c.Name,
            c.Archetype,
            c.Tier,
            c.RequiredLevel,
            c.IsAdvanced,
            c.Branch,
            c.BaseStrength,
            c.BaseAgility,
            c.BaseIntelligence,
            c.BaseWisdom,
            c.BaseCharisma,
            c.BaseEndurance,
            c.BaseLuck,
            c.Skills.Count));

        return Ok(new ApiEnvelope<object>(
            new { items = result, resistanceFormat = "multiplier" },
            new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    private static string ComputeWeakETag(string source)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        return $"W/\"{Convert.ToHexString(bytes)}\"";
    }
}

public record ClassAdminDto(
    int Id,
    string Name,
    string Archetype,
    int Tier,
    int RequiredLevel,
    bool IsAdvanced,
    int Branch,
    int BaseStrength,
    int BaseAgility,
    int BaseIntelligence,
    int BaseWisdom,
    int BaseCharisma,
    int BaseEndurance,
    int BaseLuck,
    int SkillCount);
