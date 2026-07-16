using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;
using OandaTrader.Domain.Risk;
using OandaTrader.Domain.Strategies;
using OandaTrader.Infrastructure.Data;
using OandaTrader.Infrastructure.Data.Entities;
using OandaTrader.Infrastructure.Ml;
using OandaTrader.Infrastructure.Oanda;

namespace OandaTrader.Api;

/// <summary>
/// The autonomous trading loop (see plan's "Trading Engine Flow"). Polls on a fixed interval
/// rather than scheduling precisely against candle boundaries - simple and robust, at the
/// cost of up to <see cref="PollInterval"/> latency detecting a candle close.
/// </summary>
public class TradingEngineHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OandaOptions> oandaOptions,
    IWinProbabilityPredictor predictor,
    ILogger<TradingEngineHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    /// <summary>How many recent candles to pull each tick for live signal evaluation - small
    /// and fast, so unlike backtesting this deliberately bypasses CandleCacheService (whose
    /// coverage check is tuned for wide historical ranges, not "give me anything new").</summary>
    private const int LiveCandleLookback = FeatureBuilder.MinimumCandleCount + 20;

    private readonly Dictionary<string, DateTime> _lastProcessedCandle = new();
    private decimal? _startOfDayEquity;
    private DateOnly? _startOfDayDate;
    private int _tradesOpenedToday;
    private int _closedTradesSinceLastTrain;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the host a moment to finish starting before the first tick.
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Trading engine tick failed; will retry next interval.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var oanda = scope.ServiceProvider.GetRequiredService<OandaRestClient>();
        var trainer = scope.ServiceProvider.GetRequiredService<ModelTrainingService>();

        var settings = await db.Settings.FindAsync([1], ct) ?? new Settings();
        var options = oandaOptions.Value;

        if (!options.IsConfigured || !settings.EngineEnabled)
        {
            return;
        }

        var accountSummary = await oanda.GetAccountSummaryAsync(options.AccountId, ct);
        decimal currentEquity = accountSummary.Account.Nav;
        int openPositionCount = accountSummary.Account.OpenPositionCount;

        RollDayBoundaryIfNeeded(currentEquity);

        await ReconcileClosedTradesAsync(db, oanda, options.AccountId, trainer, ct);

        var cbStatus = new CircuitBreakerStatus
        {
            StartOfDayEquity = _startOfDayEquity ?? currentEquity,
            CurrentEquity = currentEquity,
            OpenPositionCount = openPositionCount,
            TradesOpenedToday = _tradesOpenedToday,
        };
        var cbResult = CircuitBreakerEvaluator.Evaluate(cbStatus, settings);

        if (cbResult.Breached && settings.PausedReason is null)
        {
            settings.PausedReason = cbResult.Reason;
            settings.PausedAtUtc = DateTime.UtcNow;
            db.CircuitBreakerEvents.Add(new CircuitBreakerEvent
            {
                TimestampUtc = DateTime.UtcNow,
                Reason = cbResult.Reason ?? "Circuit breaker triggered.",
                TriggeredLimit = cbResult.TriggeredLimit ?? "Unknown",
            });
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Engine paused by circuit breaker: {Reason}", cbResult.Reason);
        }

        if (settings.PausedReason is not null)
        {
            return; // don't open new trades while paused; existing ones still run to their SL/TP on Oanda's side
        }

        var instruments = await db.InstrumentSettings.Where(i => i.Enabled).Select(i => i.Instrument).ToListAsync(ct);

        foreach (var instrument in instruments)
        {
            ct.ThrowIfCancellationRequested();
            await EvaluateInstrumentAsync(db, oanda, options.AccountId, instrument, settings, currentEquity, ct);
        }
    }

    private async Task EvaluateInstrumentAsync(
        AppDbContext db, OandaRestClient oanda, string accountId, string instrument,
        Settings settings, decimal currentEquity, CancellationToken ct)
    {
        var toUtc = DateTime.UtcNow;
        var fromUtc = toUtc - GranularityStep(settings.Granularity) * LiveCandleLookback;

        List<Candle> candles;
        try
        {
            candles = await oanda.GetCandlesAsync(instrument, settings.Granularity, fromUtc, toUtc, ct);
        }
        catch (OandaApiException ex)
        {
            logger.LogWarning("Failed to fetch candles for {Instrument}: {Message}", instrument, ex.Message);
            return;
        }

        if (candles.Count < FeatureBuilder.MinimumCandleCount) return;

        var lastCandle = candles[^1];
        if (_lastProcessedCandle.TryGetValue(instrument, out var lastSeen) && lastSeen >= lastCandle.TimestampUtc)
        {
            return; // already evaluated this candle
        }
        _lastProcessedCandle[instrument] = lastCandle.TimestampUtc;

        IStrategy strategy = new MlStrategy(predictor, settings.MlConfidenceThreshold);
        var decision = strategy.Evaluate(new StrategyContext { Instrument = instrument, History = candles });
        if (decision is null) return;

        decimal units;
        try
        {
            units = PositionSizer.CalculateUnits(currentEquity, settings.RiskPercentPerTrade, decision.EntryPrice, decision.StopLoss);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning("Skipping {Instrument} signal: position sizing failed ({Message})", instrument, ex.Message);
            return;
        }
        if (units <= 0) return;

        decimal signedUnits = decision.Direction == TradeDirection.Long ? units : -units;

        (string TradeId, decimal FillPrice)? fill;
        try
        {
            fill = await oanda.PlaceMarketOrderAsync(accountId, instrument, signedUnits, decision.StopLoss, decision.TakeProfit, ct);
        }
        catch (OandaApiException ex)
        {
            logger.LogError("Order placement failed for {Instrument}: {Message}", instrument, ex.Message);
            return;
        }

        if (fill is null)
        {
            logger.LogInformation("Order for {Instrument} did not fill (FOK cancel).", instrument);
            return;
        }

        db.Trades.Add(new Trade
        {
            OandaTradeId = fill.Value.TradeId,
            Instrument = instrument,
            Direction = decision.Direction,
            EntryPrice = fill.Value.FillPrice,
            EntryTimeUtc = DateTime.UtcNow,
            StopLoss = decision.StopLoss,
            TakeProfit = decision.TakeProfit,
            Units = units,
            Outcome = TradeOutcomeResult.Open,
            StrategySource = StrategySource.Live,
            FeaturesJson = System.Text.Json.JsonSerializer.Serialize(decision.Features),
            ReasoningText = decision.ReasoningText,
            MlConfidence = decision.MlConfidence,
        });
        await db.SaveChangesAsync(ct);
        _tradesOpenedToday++;

        logger.LogInformation("Opened {Direction} {Instrument} @ {Price} (trade {TradeId}, confidence {Confidence}).",
            decision.Direction, instrument, fill.Value.FillPrice, fill.Value.TradeId, decision.MlConfidence);
    }

    private async Task ReconcileClosedTradesAsync(
        AppDbContext db, OandaRestClient oanda, string accountId, ModelTrainingService trainer, CancellationToken ct)
    {
        var openDbTrades = await db.Trades
            .Where(t => t.Outcome == TradeOutcomeResult.Open && t.OandaTradeId != null)
            .ToListAsync(ct);
        if (openDbTrades.Count == 0) return;

        var openOandaIds = await oanda.GetOpenTradeIdsAsync(accountId, ct);

        int newlyClosed = 0;
        foreach (var trade in openDbTrades)
        {
            if (openOandaIds.Contains(trade.OandaTradeId!)) continue;

            var detail = await oanda.GetTradeAsync(accountId, trade.OandaTradeId!, ct);
            if (!string.Equals(detail.State, "CLOSED", StringComparison.OrdinalIgnoreCase)) continue;

            trade.ExitPrice = decimal.Parse(detail.AverageClosePrice ?? detail.Price, CultureInfo.InvariantCulture);
            trade.ExitTimeUtc = detail.CloseTime is not null ? OandaRestClient.ParseOandaTimestamp(detail.CloseTime) : DateTime.UtcNow;
            decimal realizedPl = decimal.Parse(detail.RealizedPl ?? "0", CultureInfo.InvariantCulture);
            trade.PnL = realizedPl;
            trade.Outcome = realizedPl > 0 ? TradeOutcomeResult.Win : realizedPl < 0 ? TradeOutcomeResult.Loss : TradeOutcomeResult.Breakeven;
            newlyClosed++;

            logger.LogInformation("Trade {TradeId} ({Instrument}) closed: {Outcome}, PnL {PnL}.",
                trade.OandaTradeId, trade.Instrument, trade.Outcome, realizedPl);
        }

        if (newlyClosed == 0) return;

        await db.SaveChangesAsync(ct);
        _closedTradesSinceLastTrain += newlyClosed;

        var settings = await db.Settings.FindAsync([1], ct) ?? new Settings();
        if (_closedTradesSinceLastTrain >= settings.RetrainAfterTradeCount)
        {
            _closedTradesSinceLastTrain = 0;
            logger.LogInformation("Retraining threshold reached; training a new model version.");
            var result = await trainer.TrainAsync(ct);
            if (result.Trained && result.Promoted && predictor is MlPredictionService mlPredictionService)
            {
                mlPredictionService.ReloadActiveModel();
            }
        }
    }

    private void RollDayBoundaryIfNeeded(decimal currentEquity)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (_startOfDayDate != today)
        {
            _startOfDayDate = today;
            _startOfDayEquity = currentEquity;
            _tradesOpenedToday = 0;
        }
    }

    private static TimeSpan GranularityStep(Granularity granularity) => granularity switch
    {
        Granularity.M1 => TimeSpan.FromMinutes(1),
        Granularity.M5 => TimeSpan.FromMinutes(5),
        Granularity.M15 => TimeSpan.FromMinutes(15),
        Granularity.M30 => TimeSpan.FromMinutes(30),
        Granularity.H1 => TimeSpan.FromHours(1),
        Granularity.H4 => TimeSpan.FromHours(4),
        Granularity.D => TimeSpan.FromDays(1),
        _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, null),
    };
}
