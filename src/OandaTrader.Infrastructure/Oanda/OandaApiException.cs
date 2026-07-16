namespace OandaTrader.Infrastructure.Oanda;

public class OandaApiException(System.Net.HttpStatusCode statusCode, string? oandaErrorMessage)
    : Exception(oandaErrorMessage ?? $"Oanda API request failed with status {statusCode}")
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
}
