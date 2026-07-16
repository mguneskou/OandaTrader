using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Data;

namespace OandaTrader.Api.Controllers;

public record UpdateSettingsRequest(
    decimal RiskPercentPerTrade,
    string Granularity,
    decimal MaxDailyLossPercent,
    int MaxConcurrentPositions,
    int MaxTradesPerDay,
    decimal MlConfidenceThreshold,
    int RetrainAfterTradeCount);

public record UpdateInstrumentRequest(bool Enabled);

[ApiController]
[Route("api/[controller]")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var settings = await db.Settings.FindAsync([1], ct) ?? new Settings();
        var instruments = await db.InstrumentSettings.OrderBy(i => i.Instrument).ToListAsync(ct);
        return Ok(new { Settings = settings, Instruments = instruments });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<Granularity>(request.Granularity, ignoreCase: true, out var granularity))
        {
            return BadRequest(new { error = $"Unknown granularity '{request.Granularity}'." });
        }
        if (request.RiskPercentPerTrade <= 0 || request.MlConfidenceThreshold is < 0 or > 1)
        {
            return BadRequest(new { error = "RiskPercentPerTrade must be > 0 and MlConfidenceThreshold must be between 0 and 1." });
        }

        var settings = await db.Settings.FindAsync([1], ct) ?? db.Settings.Add(new Settings()).Entity;

        settings.RiskPercentPerTrade = request.RiskPercentPerTrade;
        settings.Granularity = granularity;
        settings.MaxDailyLossPercent = request.MaxDailyLossPercent;
        settings.MaxConcurrentPositions = request.MaxConcurrentPositions;
        settings.MaxTradesPerDay = request.MaxTradesPerDay;
        settings.MlConfidenceThreshold = request.MlConfidenceThreshold;
        settings.RetrainAfterTradeCount = request.RetrainAfterTradeCount;

        await db.SaveChangesAsync(ct);
        return Ok(settings);
    }

    [HttpPut("instruments/{instrument}")]
    public async Task<IActionResult> UpdateInstrument(string instrument, [FromBody] UpdateInstrumentRequest request, CancellationToken ct)
    {
        var entity = await db.InstrumentSettings.FirstOrDefaultAsync(i => i.Instrument == instrument, ct);
        if (entity is null)
        {
            return NotFound(new { error = $"Instrument '{instrument}' is not configured." });
        }

        entity.Enabled = request.Enabled;
        await db.SaveChangesAsync(ct);
        return Ok(entity);
    }
}
