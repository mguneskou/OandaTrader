using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OandaTrader.Infrastructure.Data;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? strategySource, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var query = db.Trades.AsQueryable();
        if (!string.IsNullOrWhiteSpace(strategySource)
            && Enum.TryParse<Domain.Models.StrategySource>(strategySource, ignoreCase: true, out var source))
        {
            query = query.Where(t => t.StrategySource == source);
        }

        var trades = await query
            .OrderByDescending(t => t.EntryTimeUtc)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync(ct);

        return Ok(trades);
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpen(CancellationToken ct)
    {
        var trades = await db.Trades
            .Where(t => t.Outcome == Domain.Models.TradeOutcomeResult.Open)
            .OrderByDescending(t => t.EntryTimeUtc)
            .ToListAsync(ct);

        return Ok(trades);
    }
}
