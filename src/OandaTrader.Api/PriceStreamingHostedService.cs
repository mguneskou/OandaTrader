using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OandaTrader.Api.Realtime;
using OandaTrader.Infrastructure.Data;
using OandaTrader.Infrastructure.Oanda;

namespace OandaTrader.Api;

/// <summary>
/// Maintains Oanda's pricing stream for the enabled instruments and rebroadcasts each tick to
/// dashboard clients via SignalR. Runs whenever credentials are configured (independent of the
/// trading engine's on/off state) so the dashboard shows live prices even while paused.
/// Reconnects with capped exponential backoff on any stream drop.
/// </summary>
public class PriceStreamingHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OandaOptions> oandaOptions,
    OandaStreamingClient streamingClient,
    EngineBroadcaster broadcaster,
    ILogger<PriceStreamingHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!oandaOptions.Value.IsConfigured)
        {
            logger.LogInformation("Oanda credentials not configured; price streaming is idle.");
            return;
        }

        var backoff = MinBackoff;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var instruments = await GetEnabledInstrumentsAsync(stoppingToken);
                if (instruments.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                logger.LogInformation("Opening Oanda price stream for {Instruments}.", string.Join(", ", instruments));

                await foreach (var tick in streamingClient.StreamPricesAsync(instruments, stoppingToken))
                {
                    backoff = MinBackoff; // healthy data resets backoff
                    await broadcaster.BroadcastPriceAsync(
                        new PriceUpdate(tick.Instrument, tick.Bid, tick.Ask, tick.TimeUtc));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Price stream dropped; reconnecting in {Backoff}s.", backoff.TotalSeconds);
            }

            try
            {
                await Task.Delay(backoff, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            backoff = TimeSpan.FromSeconds(Math.Min(MaxBackoff.TotalSeconds, backoff.TotalSeconds * 2));
        }
    }

    private async Task<List<string>> GetEnabledInstrumentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.InstrumentSettings.Where(i => i.Enabled).Select(i => i.Instrument).ToListAsync(ct);
    }
}
