namespace OandaTrader.Domain.Models;

/// <summary>Candle granularity. Names match Oanda's granularity query values exactly.</summary>
public enum Granularity
{
    M1,
    M5,
    M15,
    M30,
    H1,
    H4,
    D
}
