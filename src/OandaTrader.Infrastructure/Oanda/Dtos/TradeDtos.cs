using System.Text.Json.Serialization;

namespace OandaTrader.Infrastructure.Oanda.Dtos;

public class OpenTradesResponse
{
    [JsonPropertyName("trades")]
    public List<TradeDetailDto> Trades { get; set; } = [];
}

public class TradeDetailResponse
{
    [JsonPropertyName("trade")]
    public TradeDetailDto Trade { get; set; } = new();
}

public class TradeDetailDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("instrument")]
    public string Instrument { get; set; } = "";

    [JsonPropertyName("price")]
    public string Price { get; set; } = "";

    [JsonPropertyName("openTime")]
    public string OpenTime { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("realizedPL")]
    public string? RealizedPl { get; set; }

    [JsonPropertyName("unrealizedPL")]
    public string? UnrealizedPl { get; set; }

    [JsonPropertyName("averageClosePrice")]
    public string? AverageClosePrice { get; set; }

    [JsonPropertyName("closeTime")]
    public string? CloseTime { get; set; }
}
