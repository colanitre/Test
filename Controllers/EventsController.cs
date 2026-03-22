using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgApi.Data;
using RpgApi.Models;

namespace RpgApi.Controllers;

[ApiController]
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly RpgContext _context;

    public EventsController(RpgContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IEnumerable<ProgressionEventEntry>>>> GetEvents([FromQuery] DateTime? since = null)
    {
        var list = await _context.ProgressionEvents
            .Where(e => !since.HasValue || e.Timestamp >= since.Value)
            .OrderBy(e => e.Timestamp)
            .Select(e => new ProgressionEventEntry(e.Timestamp, e.Type, e.Message, e.PlayerId, e.CharacterId, e.Metadata))
            .ToListAsync();

        return Ok(new ApiEnvelope<IEnumerable<ProgressionEventEntry>>(
            list,
            new ApiMeta(DateTime.UtcNow, HttpContext.TraceIdentifier)));
    }

    [HttpGet("stream")]
    public async Task StreamEvents([FromQuery] DateTime? since = null)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        var cancellation = HttpContext.RequestAborted;
        var cursor = since ?? DateTime.UtcNow.AddMinutes(-5);

        while (!cancellation.IsCancellationRequested)
        {
            var items = await _context.ProgressionEvents
                .Where(e => e.Timestamp >= cursor)
                .OrderBy(e => e.Timestamp)
                .Select(e => new ProgressionEventEntry(e.Timestamp, e.Type, e.Message, e.PlayerId, e.CharacterId, e.Metadata))
                .ToListAsync(cancellation);

            foreach (var item in items)
            {
                cursor = item.Timestamp.AddTicks(1);
                var json = System.Text.Json.JsonSerializer.Serialize(item);
                await Response.WriteAsync($"event: progression\n");
                await Response.WriteAsync($"data: {json}\n\n");
            }

            await Response.Body.FlushAsync(cancellation);
            await Task.Delay(1000, cancellation);
        }
    }
}
