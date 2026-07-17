namespace OandaTrader.Api.Realtime;

public record PriceUpdate(string Instrument, decimal Bid, decimal Ask, DateTime TimeUtc);

public record EngineStatusUpdate(bool EngineEnabled, string? PausedReason, DateTime? PausedAtUtc);

public record AccountUpdate(
    decimal Balance, decimal Nav, decimal UnrealizedPl, decimal MarginUsed,
    decimal MarginAvailable, int OpenTradeCount);

/// <summary>Type is "opened" or "closed".</summary>
public record TradeEvent(string Type, long TradeId, string Instrument, string Direction,
    decimal EntryPrice, decimal? ExitPrice, decimal? PnL, string Outcome, double? MlConfidence, string ReasoningText);

public record EngineLogEntry(DateTime TimestampUtc, string Level, string Message);

/// <summary>Stage is "Fetching", "Simulating", "Completed", or "Failed". Percent is 0-100
/// within the current stage (always 100 for Completed, meaningless for Failed).</summary>
public record BacktestProgressUpdate(
    Guid JobId, string Instrument, string Stage, int Percent, string? Message);
