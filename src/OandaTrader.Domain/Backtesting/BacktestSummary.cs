namespace OandaTrader.Domain.Backtesting;

public class BacktestSummary
{
    public required int TradeCount { get; init; }
    public required int Wins { get; init; }
    public required int Losses { get; init; }
    public required decimal WinRatePercent { get; init; }
    public required decimal TotalPnLInR { get; init; }
}
