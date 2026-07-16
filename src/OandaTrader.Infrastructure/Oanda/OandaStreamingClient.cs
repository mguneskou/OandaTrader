using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace OandaTrader.Infrastructure.Oanda;

/// <summary>
/// Streams live prices from Oanda's pricing stream endpoint (free on practice accounts). The
/// stream is newline-delimited JSON: PRICE objects and periodic HEARTBEATs. This client yields
/// one <see cref="PriceTick"/> per PRICE line; reconnection/backoff is the caller's job (the
/// hosted service wraps this in a reconnect loop).
/// </summary>
public class OandaStreamingClient(IHttpClientFactory httpClientFactory, IOptions<OandaOptions> options)
{
    public const string HttpClientName = "OandaStreaming";

    public async IAsyncEnumerable<PriceTick> StreamPricesAsync(
        IReadOnlyList<string> instruments, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var opts = options.Value;
        var http = httpClientFactory.CreateClient(HttpClientName);

        string instrumentList = Uri.EscapeDataString(string.Join(",", instruments));
        var url = $"v3/accounts/{opts.AccountId}/pricing/stream?instruments={instrumentList}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break; // stream closed
            if (string.IsNullOrWhiteSpace(line)) continue;

            var tick = TryParsePriceLine(line);
            if (tick is not null) yield return tick;
        }
    }

    private static PriceTick? TryParsePriceLine(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "PRICE")
            {
                return null; // HEARTBEAT or other control message
            }

            string instrument = root.GetProperty("instrument").GetString() ?? "";
            var time = OandaRestClient.ParseOandaTimestamp(root.GetProperty("time").GetString() ?? "");

            decimal bid = FirstPrice(root, "bids");
            decimal ask = FirstPrice(root, "asks");
            if (bid == 0 || ask == 0) return null;

            return new PriceTick(instrument, bid, ask, time);
        }
        catch (JsonException)
        {
            return null; // partial/malformed line; skip it
        }
    }

    private static decimal FirstPrice(JsonElement root, string side)
    {
        if (root.TryGetProperty(side, out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
        {
            var priceStr = arr[0].GetProperty("price").GetString();
            if (priceStr is not null) return decimal.Parse(priceStr, CultureInfo.InvariantCulture);
        }
        return 0;
    }
}
