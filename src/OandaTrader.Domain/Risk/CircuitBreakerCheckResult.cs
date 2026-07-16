namespace OandaTrader.Domain.Risk;

public class CircuitBreakerCheckResult
{
    public required bool Breached { get; init; }
    public string? Reason { get; init; }

    /// <summary>Name of the Settings property that tripped, e.g. nameof(Settings.MaxDailyLossPercent).</summary>
    public string? TriggeredLimit { get; init; }

    public static CircuitBreakerCheckResult Ok() => new() { Breached = false };
}
