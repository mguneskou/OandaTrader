namespace OandaTrader.Domain.Models;

/// <summary>Singleton row (Id is always 1) holding all user-configurable engine settings.</summary>
public class Settings
{
    public int Id { get; set; } = 1;

    public decimal RiskPercentPerTrade { get; set; } = 1.0m;
    public Granularity Granularity { get; set; } = Granularity.M15;

    // Circuit breakers
    public decimal MaxDailyLossPercent { get; set; } = 3.0m;
    public int MaxConcurrentPositions { get; set; } = 3;
    public int MaxTradesPerDay { get; set; } = 10;

    public decimal MlConfidenceThreshold { get; set; } = 0.55m;
    public int RetrainAfterTradeCount { get; set; } = 25;

    /// <summary>Whether the autonomous trading loop is allowed to run. Off by default until you turn it on.</summary>
    public bool EngineEnabled { get; set; } = false;

    /// <summary>Non-null when a circuit breaker has paused trading; cleared on manual resume.</summary>
    public string? PausedReason { get; set; }
    public DateTime? PausedAtUtc { get; set; }
}
