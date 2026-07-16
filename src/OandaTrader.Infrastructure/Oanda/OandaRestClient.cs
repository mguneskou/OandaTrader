using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Oanda.Dtos;

namespace OandaTrader.Infrastructure.Oanda;

public class OandaRestClient(HttpClient httpClient)
{
    /// <summary>Oanda's max candles per request when paging with `count`.</summary>
    private const int MaxCandlesPerRequest = 5000;

    public async Task<AccountSummaryResponse> GetAccountSummaryAsync(string accountId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"v3/accounts/{accountId}/summary", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<AccountSummaryResponse>(cancellationToken: ct)
               ?? throw new OandaApiException(response.StatusCode, "Oanda returned an empty account summary response.");
    }

    /// <summary>Fetches completed candles in [fromUtc, toUtc), paging in chunks of up to 5000
    /// since Oanda rejects a single request spanning more candles than that.</summary>
    public async Task<List<Candle>> GetCandlesAsync(
        string instrument, Granularity granularity, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var result = new List<Candle>();
        var cursor = fromUtc;

        while (cursor < toUtc)
        {
            string fromParam = Uri.EscapeDataString(ToOandaTimestamp(cursor));
            var url = $"v3/instruments/{instrument}/candles?granularity={granularity}&price=M&count={MaxCandlesPerRequest}&from={fromParam}";

            using var response = await httpClient.GetAsync(url, ct);
            await EnsureSuccessAsync(response, ct);
            var body = await response.Content.ReadFromJsonAsync<CandlesResponse>(cancellationToken: ct) ?? new CandlesResponse();

            if (body.Candles.Count == 0)
            {
                break;
            }

            foreach (var c in body.Candles)
            {
                if (!c.Complete || c.Mid is null) continue;

                var timestamp = ParseOandaTimestamp(c.Time);
                if (timestamp >= toUtc) continue;

                result.Add(new Candle
                {
                    Instrument = instrument,
                    Granularity = granularity,
                    TimestampUtc = timestamp,
                    Open = decimal.Parse(c.Mid.O, CultureInfo.InvariantCulture),
                    High = decimal.Parse(c.Mid.H, CultureInfo.InvariantCulture),
                    Low = decimal.Parse(c.Mid.L, CultureInfo.InvariantCulture),
                    Close = decimal.Parse(c.Mid.C, CultureInfo.InvariantCulture),
                    Volume = c.Volume,
                });
            }

            var lastReturnedTime = ParseOandaTimestamp(body.Candles[^1].Time);
            if (lastReturnedTime < cursor)
            {
                break; // no forward progress; avoid looping forever
            }

            // Advance by a full granularity step, not a fixed offset: Oanda aligns `from` down
            // to the nearest candle boundary for the requested granularity, so a sub-boundary
            // nudge (e.g. +1 second) gets floored right back to the candle we just fetched,
            // which re-returns it on the next page and trips the Candles unique index.
            cursor = lastReturnedTime + GranularityStep(granularity);

            if (body.Candles.Count < MaxCandlesPerRequest)
            {
                break; // fewer than the max came back, so we've caught up to the available data
            }
        }

        return result
            .DistinctBy(c => c.TimestampUtc) // defense in depth against any remaining boundary overlap
            .OrderBy(c => c.TimestampUtc)
            .ToList();
    }

    private static TimeSpan GranularityStep(Granularity granularity) => granularity switch
    {
        Granularity.M1 => TimeSpan.FromMinutes(1),
        Granularity.M5 => TimeSpan.FromMinutes(5),
        Granularity.M15 => TimeSpan.FromMinutes(15),
        Granularity.M30 => TimeSpan.FromMinutes(30),
        Granularity.H1 => TimeSpan.FromHours(1),
        Granularity.H4 => TimeSpan.FromHours(4),
        Granularity.D => TimeSpan.FromDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, null),
    };

    private static string ToOandaTimestamp(DateTime utc) =>
        utc.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture);

    private static DateTime ParseOandaTimestamp(string time) =>
        DateTime.Parse(time, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? errorMessage = null;
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errorMessage", out var msg))
            {
                errorMessage = msg.GetString();
            }
        }
        catch (JsonException)
        {
            // Response body wasn't the expected Oanda error JSON shape; fall back to the status code.
        }

        throw new OandaApiException(response.StatusCode, errorMessage);
    }
}
