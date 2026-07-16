using OandaTrader.Domain.Models;
using OandaTrader.Domain.Risk;
using Xunit;

namespace OandaTrader.Domain.Tests.Risk;

public class CircuitBreakerEvaluatorTests
{
    private static Settings DefaultSettings() => new()
    {
        MaxDailyLossPercent = 3.0m,
        MaxConcurrentPositions = 3,
        MaxTradesPerDay = 10,
    };

    [Fact]
    public void Evaluate_WithinAllLimits_IsNotBreached()
    {
        var status = new CircuitBreakerStatus
        {
            StartOfDayEquity = 10_000m,
            CurrentEquity = 10_050m, // up on the day
            OpenPositionCount = 1,
            TradesOpenedToday = 2,
        };

        var result = CircuitBreakerEvaluator.Evaluate(status, DefaultSettings());

        Assert.False(result.Breached);
    }

    [Fact]
    public void Evaluate_DailyLossAtLimit_IsBreached()
    {
        var status = new CircuitBreakerStatus
        {
            StartOfDayEquity = 10_000m,
            CurrentEquity = 9_700m, // exactly 3% down
            OpenPositionCount = 0,
            TradesOpenedToday = 0,
        };

        var result = CircuitBreakerEvaluator.Evaluate(status, DefaultSettings());

        Assert.True(result.Breached);
        Assert.Equal(nameof(Settings.MaxDailyLossPercent), result.TriggeredLimit);
    }

    [Fact]
    public void Evaluate_DailyLossBelowLimit_IsNotBreached()
    {
        var status = new CircuitBreakerStatus
        {
            StartOfDayEquity = 10_000m,
            CurrentEquity = 9_750m, // 2.5% down, under the 3% limit
            OpenPositionCount = 0,
            TradesOpenedToday = 0,
        };

        var result = CircuitBreakerEvaluator.Evaluate(status, DefaultSettings());

        Assert.False(result.Breached);
    }

    [Fact]
    public void Evaluate_MaxConcurrentPositionsReached_IsBreached()
    {
        var status = new CircuitBreakerStatus
        {
            StartOfDayEquity = 10_000m,
            CurrentEquity = 10_000m,
            OpenPositionCount = 3,
            TradesOpenedToday = 0,
        };

        var result = CircuitBreakerEvaluator.Evaluate(status, DefaultSettings());

        Assert.True(result.Breached);
        Assert.Equal(nameof(Settings.MaxConcurrentPositions), result.TriggeredLimit);
    }

    [Fact]
    public void Evaluate_MaxTradesPerDayReached_IsBreached()
    {
        var status = new CircuitBreakerStatus
        {
            StartOfDayEquity = 10_000m,
            CurrentEquity = 10_000m,
            OpenPositionCount = 0,
            TradesOpenedToday = 10,
        };

        var result = CircuitBreakerEvaluator.Evaluate(status, DefaultSettings());

        Assert.True(result.Breached);
        Assert.Equal(nameof(Settings.MaxTradesPerDay), result.TriggeredLimit);
    }
}
