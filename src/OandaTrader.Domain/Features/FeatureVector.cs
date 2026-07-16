namespace OandaTrader.Domain.Features;

/// <summary>
/// The flat feature schema computed at every trade decision point. Used identically by
/// BaselineStrategy (for signal context), BacktestEngine (to build the ML training set), and
/// MlStrategy (as the live prediction input) so training and serving never drift apart.
/// </summary>
public class FeatureVector
{
    public double Ema20 { get; set; }
    public double Ema50 { get; set; }
    public double Rsi14 { get; set; }
    public double Atr14 { get; set; }
    public double MacdLine { get; set; }
    public double MacdSignal { get; set; }
    public double MacdHistogram { get; set; }
    public double BollingerWidth20 { get; set; }
    public double PriceDistanceFromEma20Pct { get; set; }
    public double RecentVolatility20 { get; set; }
    public double HourOfDayUtc { get; set; }
    public double DayOfWeek { get; set; }
}
