using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Data;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(AppDbContext db) : ControllerBase
{
    /// <summary>Aggregate performance stats for the dashboard. Defaults to live trades; pass
    /// ?strategySource=Backtest to inspect the bootstrap dataset instead.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string strategySource = "Live", CancellationToken ct = default)
    {
        if (!Enum.TryParse<StrategySource>(strategySource, ignoreCase: true, out var source))
        {
            return BadRequest(new { error = $"Unknown strategySource '{strategySource}'." });
        }

        var closed = await db.Trades
            .Where(t => t.StrategySource == source && t.Outcome != TradeOutcomeResult.Open)
            .OrderBy(t => t.ExitTimeUtc ?? t.EntryTimeUtc)
            .ToListAsync(ct);

        int wins = closed.Count(t => t.Outcome == TradeOutcomeResult.Win);
        int losses = closed.Count(t => t.Outcome == TradeOutcomeResult.Loss);
        decimal totalPnL = closed.Sum(t => t.PnL ?? 0);

        // Cumulative PnL curve (index-based x; the client can relabel by time if desired).
        decimal running = 0;
        var equityCurve = closed.Select((t, i) =>
        {
            running += t.PnL ?? 0;
            return new { index = i + 1, time = t.ExitTimeUtc ?? t.EntryTimeUtc, cumulativePnL = running };
        }).ToList();

        var perInstrument = closed
            .GroupBy(t => t.Instrument)
            .Select(g => new
            {
                instrument = g.Key,
                trades = g.Count(),
                wins = g.Count(t => t.Outcome == TradeOutcomeResult.Win),
                winRatePercent = g.Any() ? (decimal)g.Count(t => t.Outcome == TradeOutcomeResult.Win) / g.Count() * 100m : 0,
                totalPnL = g.Sum(t => t.PnL ?? 0),
            })
            .OrderByDescending(x => x.trades)
            .ToList();

        return Ok(new
        {
            source = source.ToString(),
            tradeCount = closed.Count,
            wins,
            losses,
            winRatePercent = closed.Count > 0 ? (decimal)wins / closed.Count * 100m : 0,
            totalPnL,
            equityCurve,
            perInstrument,
        });
    }

    [HttpGet("circuit-breaker-events")]
    public async Task<IActionResult> GetCircuitBreakerEvents(CancellationToken ct)
    {
        var events = await db.CircuitBreakerEvents
            .OrderByDescending(e => e.TimestampUtc)
            .Take(50)
            .ToListAsync(ct);
        return Ok(events);
    }
}
