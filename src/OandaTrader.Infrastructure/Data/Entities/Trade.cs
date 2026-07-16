using OandaTrader.Domain.Models;

namespace OandaTrader.Infrastructure.Data.Entities;

/// <summary>A persisted trade record — covers both live trades (placed on Oanda) and
/// synthetic backtest trades (used to bootstrap ML training data). See plan's cold-start
/// section: backtest trades are tagged StrategySource=Backtest, live ones Live.</summary>
public class Trade
{
    public long Id { get; set; }

    /// <summary>Null for backtest-only (synthetic) trades.</summary>
    public string? OandaTradeId { get; set; }

    public string Instrument { get; set; } = "";
    public TradeDirection Direction { get; set; }

    public decimal EntryPrice { get; set; }
    public DateTime EntryTimeUtc { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TakeProfit { get; set; }
    public decimal Units { get; set; }

    public decimal? ExitPrice { get; set; }
    public DateTime? ExitTimeUtc { get; set; }
    public decimal? PnL { get; set; }

    public TradeOutcomeResult Outcome { get; set; } = TradeOutcomeResult.Open;
    public StrategySource StrategySource { get; set; }

    /// <summary>JSON snapshot of the feature vector FeatureBuilder computed at decision time —
    /// this is the training sample used when the ML model is (re)trained.</summary>
    public string FeaturesJson { get; set; } = "{}";

    public string ReasoningText { get; set; } = "";

    /// <summary>The active model's predicted win-probability at decision time, if MlStrategy was used.</summary>
    public double? MlConfidence { get; set; }
}
