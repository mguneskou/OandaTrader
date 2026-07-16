using System.Net.Http.Json;
using System.Text.Json;
using OandaTrader.Infrastructure.Oanda.Dtos;

namespace OandaTrader.Infrastructure.Oanda;

public class OandaRestClient(HttpClient httpClient)
{
    public async Task<AccountSummaryResponse> GetAccountSummaryAsync(string accountId, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"v3/accounts/{accountId}/summary", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<AccountSummaryResponse>(cancellationToken: ct)
               ?? throw new OandaApiException(response.StatusCode, "Oanda returned an empty account summary response.");
    }

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
