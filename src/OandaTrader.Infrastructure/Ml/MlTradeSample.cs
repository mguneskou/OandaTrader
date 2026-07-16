using OandaTrader.Domain.Features;

namespace OandaTrader.Infrastructure.Ml;

/// <summary>ML.NET training/scoring row. Mirrors FeatureVector exactly (ML.NET wants floats),
/// with the label being whether the trade won.</summary>
public class MlTradeSample
{
    public bool Label { get; set; }

    public float Ema20 { get; set; }
    public float Ema50 { get; set; }
    public float Rsi14 { get; set; }
    public float Atr14 { get; set; }
    public float MacdLine { get; set; }
    public float MacdSignal { get; set; }
    public float MacdHistogram { get; set; }
    public float BollingerWidth20 { get; set; }
    public float PriceDistanceFromEma20Pct { get; set; }
    public float RecentVolatility20 { get; set; }
    public float HourOfDayUtc { get; set; }
    public float DayOfWeek { get; set; }

    public static readonly string[] FeatureColumnNames =
    [
        nameof(Ema20), nameof(Ema50), nameof(Rsi14), nameof(Atr14),
        nameof(MacdLine), nameof(MacdSignal), nameof(MacdHistogram),
        nameof(BollingerWidth20), nameof(PriceDistanceFromEma20Pct),
        nameof(RecentVolatility20), nameof(HourOfDayUtc), nameof(DayOfWeek),
    ];

    public static MlTradeSample FromFeatures(FeatureVector f, bool label = false) => new()
    {
        Label = label,
        Ema20 = (float)f.Ema20,
        Ema50 = (float)f.Ema50,
        Rsi14 = (float)f.Rsi14,
        Atr14 = (float)f.Atr14,
        MacdLine = (float)f.MacdLine,
        MacdSignal = (float)f.MacdSignal,
        MacdHistogram = (float)f.MacdHistogram,
        BollingerWidth20 = (float)f.BollingerWidth20,
        PriceDistanceFromEma20Pct = (float)f.PriceDistanceFromEma20Pct,
        RecentVolatility20 = (float)f.RecentVolatility20,
        HourOfDayUtc = (float)f.HourOfDayUtc,
        DayOfWeek = (float)f.DayOfWeek,
    };
}

public class MlTradePrediction
{
    public bool PredictedLabel { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}
