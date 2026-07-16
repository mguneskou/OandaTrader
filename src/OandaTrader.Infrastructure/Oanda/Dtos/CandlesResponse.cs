using System.Text.Json.Serialization;

namespace OandaTrader.Infrastructure.Oanda.Dtos;

public class CandlesResponse
{
    [JsonPropertyName("candles")]
    public List<CandleDto> Candles { get; set; } = [];
}

public class CandleDto
{
    [JsonPropertyName("complete")]
    public bool Complete { get; set; }

    [JsonPropertyName("volume")]
    public long Volume { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; } = "";

    [JsonPropertyName("mid")]
    public CandleMidDto? Mid { get; set; }
}

public class CandleMidDto
{
    [JsonPropertyName("o")]
    public string O { get; set; } = "";

    [JsonPropertyName("h")]
    public string H { get; set; } = "";

    [JsonPropertyName("l")]
    public string L { get; set; } = "";

    [JsonPropertyName("c")]
    public string C { get; set; } = "";
}
