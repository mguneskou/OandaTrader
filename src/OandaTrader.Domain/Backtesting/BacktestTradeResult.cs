using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Backtesting;

public class BacktestTradeResult
{
    public required string Instrument { get; init; }
    public required TradeDirection Direction { get; init; }
    public required decimal EntryPrice { get; init; }
    public required DateTime EntryTimeUtc { get; init; }
    public required decimal StopLoss { get; init; }
    public required decimal TakeProfit { get; init; }
    public required decimal ExitPrice { get; init; }
    public required DateTime ExitTimeUtc { get; init; }
    public required TradeOutcomeResult Outcome { get; init; }
    public required FeatureVector Features { get; init; }
    public required string ReasoningText { get; init; }

    /// <summary>PnL in R-multiples (reward relative to the risked stop distance) — avoids
    /// needing an account-equity assumption for a backtest that only exists to bootstrap
    /// ML training data.</summary>
    public required decimal PnLInR { get; init; }
}
