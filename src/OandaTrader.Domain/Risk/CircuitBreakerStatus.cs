namespace OandaTrader.Domain.Risk;

/// <summary>Current account/trading state fed to the circuit breaker check on every equity
/// update and before every new order.</summary>
public class CircuitBreakerStatus
{
    public required decimal StartOfDayEquity { get; init; }
    public required decimal CurrentEquity { get; init; }
    public required int OpenPositionCount { get; init; }
    public required int TradesOpenedToday { get; init; }
}
