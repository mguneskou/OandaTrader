namespace OandaTrader.Infrastructure.Oanda;

public record PriceTick(string Instrument, decimal Bid, decimal Ask, DateTime TimeUtc);
