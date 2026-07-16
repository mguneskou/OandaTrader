using OandaTrader.Domain.Models;

namespace OandaTrader.Domain.Risk;

public static class CircuitBreakerEvaluator
{
    public static CircuitBreakerCheckResult Evaluate(CircuitBreakerStatus status, Settings settings)
    {
        if (status.StartOfDayEquity > 0)
        {
            decimal drawdownPercent = (status.StartOfDayEquity - status.CurrentEquity) / status.StartOfDayEquity * 100m;
            if (drawdownPercent >= settings.MaxDailyLossPercent)
            {
                return new CircuitBreakerCheckResult
                {
                    Breached = true,
                    TriggeredLimit = nameof(Settings.MaxDailyLossPercent),
                    Reason = $"Daily loss {drawdownPercent:F2}% has reached the {settings.MaxDailyLossPercent}% limit."
                };
            }
        }

        if (status.OpenPositionCount >= settings.MaxConcurrentPositions)
        {
            return new CircuitBreakerCheckResult
            {
                Breached = true,
                TriggeredLimit = nameof(Settings.MaxConcurrentPositions),
                Reason = $"{status.OpenPositionCount} open positions have reached the {settings.MaxConcurrentPositions} limit."
            };
        }

        if (status.TradesOpenedToday >= settings.MaxTradesPerDay)
        {
            return new CircuitBreakerCheckResult
            {
                Breached = true,
                TriggeredLimit = nameof(Settings.MaxTradesPerDay),
                Reason = $"{status.TradesOpenedToday} trades opened today have reached the {settings.MaxTradesPerDay} limit."
            };
        }

        return CircuitBreakerCheckResult.Ok();
    }
}
