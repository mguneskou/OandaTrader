using System.Text.Json.Serialization;

namespace OandaTrader.Infrastructure.Oanda.Dtos;

public class CreateOrderResponse
{
    [JsonPropertyName("orderFillTransaction")]
    public OrderFillTransactionDto? OrderFillTransaction { get; set; }

    [JsonPropertyName("orderCancelTransaction")]
    public OrderCancelTransactionDto? OrderCancelTransaction { get; set; }

    [JsonPropertyName("orderRejectTransaction")]
    public OrderCancelTransactionDto? OrderRejectTransaction { get; set; }
}

public class OrderFillTransactionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("price")]
    public string Price { get; set; } = "";

    [JsonPropertyName("tradeOpened")]
    public TradeOpenedDto? TradeOpened { get; set; }
}

public class TradeOpenedDto
{
    [JsonPropertyName("tradeID")]
    public string TradeId { get; set; } = "";
}

public class OrderCancelTransactionDto
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = "";
}
