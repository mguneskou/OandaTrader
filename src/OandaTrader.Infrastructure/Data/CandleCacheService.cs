using Microsoft.EntityFrameworkCore;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Oanda;

namespace OandaTrader.Infrastructure.Data;

/// <summary>Local cache over the Candles table (see plan's data model) so repeated backtests
/// over the same range don't keep re-fetching from Oanda.</summary>
public class CandleCacheService(AppDbContext db, OandaRestClient oanda)
{
    public async Task<List<Candle>> GetCandlesAsync(
        string instrument, Granularity granularity, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var cached = await db.Candles
            .Where(c => c.Instrument == instrument && c.Granularity == granularity
                        && c.TimestampUtc >= fromUtc && c.TimestampUtc < toUtc)
            .OrderBy(c => c.TimestampUtc)
            .ToListAsync(ct);

        bool coversRequestedRange = cached.Count > 0
            && cached[0].TimestampUtc <= fromUtc.AddHours(4)
            && cached[^1].TimestampUtc >= toUtc.AddDays(-1);

        if (coversRequestedRange)
        {
            return cached;
        }

        var fetched = await oanda.GetCandlesAsync(instrument, granularity, fromUtc, toUtc, ct);
        await UpsertAsync(fetched, ct);
        return fetched;
    }

    private async Task UpsertAsync(List<Candle> candles, CancellationToken ct)
    {
        if (candles.Count == 0) return;

        string instrument = candles[0].Instrument;
        Granularity granularity = candles[0].Granularity;
        DateTime minTime = candles.Min(c => c.TimestampUtc);
        DateTime maxTime = candles.Max(c => c.TimestampUtc);

        var existingTimestamps = (await db.Candles
                .Where(c => c.Instrument == instrument && c.Granularity == granularity
                            && c.TimestampUtc >= minTime && c.TimestampUtc <= maxTime)
                .Select(c => c.TimestampUtc)
                .ToListAsync(ct))
            .ToHashSet();

        var newCandles = candles
            .Where(c => !existingTimestamps.Contains(c.TimestampUtc))
            .DistinctBy(c => c.TimestampUtc)
            .ToList();
        if (newCandles.Count > 0)
        {
            db.Candles.AddRange(newCandles);
            await db.SaveChangesAsync(ct);
        }
    }
}
