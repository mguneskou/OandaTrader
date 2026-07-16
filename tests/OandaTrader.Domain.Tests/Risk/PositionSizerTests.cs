using OandaTrader.Domain.Risk;
using Xunit;

namespace OandaTrader.Domain.Tests.Risk;

public class PositionSizerTests
{
    [Fact]
    public void CalculateUnits_KnownExample_MatchesExpectedUnits()
    {
        // Risking 1% of 10,000 = 100. Stop distance 0.0050 => 100 / 0.0050 = 20,000 units.
        decimal units = PositionSizer.CalculateUnits(
            accountEquity: 10_000m, riskPercent: 1m, entryPrice: 1.1000m, stopLoss: 1.0950m);

        Assert.Equal(20_000m, units);
    }

    [Fact]
    public void CalculateUnits_WorksForShortStops_UsesAbsoluteDistance()
    {
        decimal units = PositionSizer.CalculateUnits(
            accountEquity: 10_000m, riskPercent: 1m, entryPrice: 1.1000m, stopLoss: 1.1050m);

        Assert.Equal(20_000m, units);
    }

    [Fact]
    public void CalculateUnits_FractionalResult_FlooredToWholeUnits()
    {
        // riskAmount = 10,000 * 1% = 100; stopDistance = 0.00003 => 100 / 0.00003 = 3,333,333.33...
        decimal units = PositionSizer.CalculateUnits(
            accountEquity: 10_000m, riskPercent: 1m, entryPrice: 1.10000m, stopLoss: 1.09997m);

        Assert.Equal(3_333_333m, units);
    }

    [Theory]
    [InlineData(0, 1, 1.1000, 1.0950)]
    [InlineData(-100, 1, 1.1000, 1.0950)]
    public void CalculateUnits_NonPositiveEquity_Throws(decimal equity, decimal riskPercent, decimal entry, decimal stop)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionSizer.CalculateUnits(equity, riskPercent, entry, stop));
    }

    [Theory]
    [InlineData(10_000, 0, 1.1000, 1.0950)]
    [InlineData(10_000, -1, 1.1000, 1.0950)]
    public void CalculateUnits_NonPositiveRiskPercent_Throws(decimal equity, decimal riskPercent, decimal entry, decimal stop)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionSizer.CalculateUnits(equity, riskPercent, entry, stop));
    }

    [Fact]
    public void CalculateUnits_ZeroStopDistance_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PositionSizer.CalculateUnits(10_000m, 1m, 1.1000m, 1.1000m));
    }
}
