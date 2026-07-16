using OandaTrader.Domain.Models;

namespace OandaTrader.Infrastructure.Data.Entities;

public class BacktestRun
{
    public int Id { get; set; }
    public DateTime RunAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Instrument { get; set; } = "";
    public Granularity Granularity { get; set; }

    /// <summary>JSON blob: trade count, win rate, total P&amp;L, etc.</summary>
    public string ResultSummaryJson { get; set; } = "{}";
}
