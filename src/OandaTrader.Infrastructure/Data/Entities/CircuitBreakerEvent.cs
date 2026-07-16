namespace OandaTrader.Infrastructure.Data.Entities;

public class CircuitBreakerEvent
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Reason { get; set; } = "";

    /// <summary>Which configured limit tripped, e.g. "MaxDailyLossPercent".</summary>
    public string TriggeredLimit { get; set; } = "";

    public DateTime? ResumedAtUtc { get; set; }
}
