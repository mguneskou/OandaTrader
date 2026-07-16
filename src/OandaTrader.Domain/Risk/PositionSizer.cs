namespace OandaTrader.Domain.Risk;

/// <summary>Fixed-fractional position sizing: risk a configured percent of account equity
/// per trade, sized off the stop-loss distance.</summary>
public static class PositionSizer
{
    public static decimal CalculateUnits(decimal accountEquity, decimal riskPercent, decimal entryPrice, decimal stopLoss)
    {
        if (accountEquity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(accountEquity), "Account equity must be greater than zero.");
        }
        if (riskPercent <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(riskPercent), "Risk percent must be greater than zero.");
        }

        decimal stopDistance = Math.Abs(entryPrice - stopLoss);
        if (stopDistance <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stopLoss), "Stop-loss distance must be greater than zero.");
        }

        decimal riskAmount = accountEquity * (riskPercent / 100m);
        return Math.Floor(riskAmount / stopDistance);
    }
}
