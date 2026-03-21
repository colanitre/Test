using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;

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
