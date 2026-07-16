namespace OandaTrader.Infrastructure.Data.Entities;

public class InstrumentSetting
{
    public int Id { get; set; }

    /// <summary>Oanda instrument name, e.g. "EUR_USD".</summary>
    public string Instrument { get; set; } = "";

    public bool Enabled { get; set; } = true;
}
