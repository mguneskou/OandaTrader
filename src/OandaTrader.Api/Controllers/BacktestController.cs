using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Backtesting;
using OandaTrader.Infrastructure.Data;
using OandaTrader.Infrastructure.Oanda;

namespace OandaTrader.Api.Controllers;

public record RunBacktestRequest(string Instrument, int Months = 6, string? Granularity = null);

[ApiController]
[Route("api/[controller]")]
public class BacktestController(BacktestRunner runner, AppDbContext db) : ControllerBase
{
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] RunBacktestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Instrument))
        {
            return BadRequest(new { error = "Instrument is required." });
        }

        Granularity granularity;
        if (request.Granularity is null)
        {
            var settings = await db.Settings.FindAsync([1], ct) ?? new Settings();
            granularity = settings.Granularity;
        }
        else if (!Enum.TryParse(request.Granularity, ignoreCase: true, out granularity))
        {
            return BadRequest(new { error = $"Unknown granularity '{request.Granularity}'." });
        }

        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddMonths(-Math.Max(1, request.Months));

        try
        {
            var result = await runner.RunAsync(request.Instrument, granularity, fromUtc, toUtc, ct);
            return Ok(new
            {
                result.Run.Id,
                result.Run.Instrument,
                result.Run.Granularity,
                result.Run.StartDate,
                result.Run.EndDate,
                Summary = result.Summary,
            });
        }
        catch (OandaApiException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns(CancellationToken ct)
    {
        var runs = await db.BacktestRuns.OrderByDescending(r => r.RunAtUtc).ToListAsync(ct);
        return Ok(runs);
    }
}
