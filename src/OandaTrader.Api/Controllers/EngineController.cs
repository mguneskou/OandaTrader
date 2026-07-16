using Microsoft.AspNetCore.Mvc;
using OandaTrader.Api.Realtime;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Data;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EngineController(AppDbContext db, EngineBroadcaster broadcaster) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var settings = await GetOrCreateSettingsAsync(ct);
        return Ok(new
        {
            settings.EngineEnabled,
            settings.PausedReason,
            settings.PausedAtUtc,
        });
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(CancellationToken ct)
    {
        var settings = await GetOrCreateSettingsAsync(ct);
        settings.EngineEnabled = true;
        await db.SaveChangesAsync(ct);
        await BroadcastStatusAsync(settings);
        return Ok(new { settings.EngineEnabled });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> Stop(CancellationToken ct)
    {
        var settings = await GetOrCreateSettingsAsync(ct);
        settings.EngineEnabled = false;
        await db.SaveChangesAsync(ct);
        await BroadcastStatusAsync(settings);
        return Ok(new { settings.EngineEnabled });
    }

    /// <summary>Clears a circuit-breaker pause. Per the plan, pauses require explicit user
    /// action - the engine never clears this on its own.</summary>
    [HttpPost("resume")]
    public async Task<IActionResult> Resume(CancellationToken ct)
    {
        var settings = await GetOrCreateSettingsAsync(ct);
        settings.PausedReason = null;
        settings.PausedAtUtc = null;
        await db.SaveChangesAsync(ct);
        await BroadcastStatusAsync(settings);
        return Ok(new { settings.PausedReason });
    }

    private Task BroadcastStatusAsync(Settings settings) =>
        broadcaster.BroadcastEngineStatusAsync(new EngineStatusUpdate(
            settings.EngineEnabled, settings.PausedReason, settings.PausedAtUtc));

    private async Task<Settings> GetOrCreateSettingsAsync(CancellationToken ct)
    {
        var settings = await db.Settings.FindAsync([1], ct);
        if (settings is not null) return settings;

        settings = new Settings();
        db.Settings.Add(settings);
        await db.SaveChangesAsync(ct);
        return settings;
    }
}
