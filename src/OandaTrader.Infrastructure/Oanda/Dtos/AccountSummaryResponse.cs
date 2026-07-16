using System.Text.Json.Serialization;

namespace OandaTrader.Infrastructure.Oanda.Dtos;

public class AccountSummaryResponse
{
    [JsonPropertyName("account")]
    public AccountSummary Account { get; set; } = new();

    [JsonPropertyName("lastTransactionID")]
    public string LastTransactionId { get; set; } = "";
}

public class AccountSummary
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }

    [JsonPropertyName("NAV")]
    public decimal Nav { get; set; }

    [JsonPropertyName("unrealizedPL")]
    public decimal UnrealizedPl { get; set; }

    [JsonPropertyName("marginUsed")]
    public decimal MarginUsed { get; set; }

    [JsonPropertyName("marginAvailable")]
    public decimal MarginAvailable { get; set; }

    [JsonPropertyName("openTradeCount")]
    public int OpenTradeCount { get; set; }

    [JsonPropertyName("openPositionCount")]
    public int OpenPositionCount { get; set; }

    [JsonPropertyName("pendingOrderCount")]
    public int PendingOrderCount { get; set; }
}
