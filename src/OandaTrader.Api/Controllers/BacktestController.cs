using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OandaTrader.Api.Realtime;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Backtesting;
using OandaTrader.Infrastructure.Data;
using OandaTrader.Infrastructure.Oanda;

namespace OandaTrader.Api.Controllers;

public record RunBacktestRequest(string Instrument, int Months = 6, string? Granularity = null);

[ApiController]
[Route("api/[controller]")]
public class BacktestController(
    AppDbContext db,
    EngineBroadcaster broadcaster,
    IServiceScopeFactory scopeFactory,
    BacktestJobTracker jobTracker,
    ILogger<BacktestController> logger) : ControllerBase
{
    /// <summary>Kicks the backtest off in the background and returns immediately - a 6-12
    /// month run takes long enough that holding the HTTP request open isn't a good fit.
    /// Progress streams over SignalR as BacktestProgress events keyed by the returned jobId.</summary>
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

        if (!jobTracker.TryStart(request.Instrument))
        {
            return Conflict(new { error = $"A backtest for {request.Instrument} is already running." });
        }

        var jobId = Guid.NewGuid();
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc.AddMonths(-Math.Max(1, request.Months));

        // Fire-and-forget on the app's own scope factory (not HttpContext.RequestServices,
        // which gets disposed the moment this action returns). Errors are caught and reported
        // over SignalR instead of propagating - there's no HTTP response left to carry them.
        _ = RunInBackgroundAsync(jobId, request.Instrument, granularity, fromUtc, toUtc);

        return Accepted(new { jobId });
    }

    private async Task RunInBackgroundAsync(Guid jobId, string instrument, Granularity granularity, DateTime fromUtc, DateTime toUtc)
    {
        using var scope = scopeFactory.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<BacktestRunner>();

        try
        {
            await runner.RunAsync(
                instrument, granularity, fromUtc, toUtc,
                onProgress: (stage, percent) =>
                    _ = broadcaster.BroadcastBacktestProgressAsync(new BacktestProgressUpdate(jobId, instrument, stage, percent, null)),
                ct: CancellationToken.None);

            await broadcaster.BroadcastBacktestProgressAsync(
                new BacktestProgressUpdate(jobId, instrument, "Completed", 100, null));
        }
        catch (OandaApiException ex)
        {
            logger.LogWarning(ex, "Backtest {JobId} for {Instrument} failed.", jobId, instrument);
            await broadcaster.BroadcastBacktestProgressAsync(
                new BacktestProgressUpdate(jobId, instrument, "Failed", 0, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backtest {JobId} for {Instrument} failed unexpectedly.", jobId, instrument);
            await broadcaster.BroadcastBacktestProgressAsync(
                new BacktestProgressUpdate(jobId, instrument, "Failed", 0, ex.Message));
        }
        finally
        {
            jobTracker.Finish(instrument);
        }
    }

    [HttpGet("runs")]
    public async Task<IActionResult> GetRuns(CancellationToken ct)
    {
        var runs = await db.BacktestRuns.OrderByDescending(r => r.RunAtUtc).ToListAsync(ct);
        return Ok(runs);
    }
}
