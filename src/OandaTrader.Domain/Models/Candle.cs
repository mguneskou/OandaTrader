namespace OandaTrader.Domain.Models;

/// <summary>An OHLC candle. Sourced from Oanda's REST candles endpoint (see plan: streaming
/// is used for live tick prices/P&amp;L, but candles are the authoritative OHLC for strategy signals).</summary>
public class Candle
{
    public long Id { get; set; }
    public string Instrument { get; set; } = "";
    public Granularity Granularity { get; set; }
    public DateTime TimestampUtc { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}
