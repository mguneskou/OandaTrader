using System.Collections.Concurrent;

namespace OandaTrader.Api.Realtime;

public record EngineStateSnapshot(
    EngineStatusUpdate? EngineStatus,
    AccountUpdate? Account,
    IReadOnlyDictionary<string, PriceUpdate> Prices);

/// <summary>Holds the latest known engine status, account summary, and per-instrument prices
/// so a newly-connected SignalR client can be handed the current state immediately instead of
/// waiting for the next push. Singleton, thread-safe.</summary>
public class EngineStateCache
{
    private readonly ConcurrentDictionary<string, PriceUpdate> _prices = new();
    private EngineStatusUpdate? _engineStatus;
    private AccountUpdate? _account;

    public void SetEngineStatus(EngineStatusUpdate status) => _engineStatus = status;
    public void SetAccount(AccountUpdate account) => _account = account;
    public void SetPrice(PriceUpdate price) => _prices[price.Instrument] = price;

    public EngineStateSnapshot Snapshot() =>
        new(_engineStatus, _account, new Dictionary<string, PriceUpdate>(_prices));
}
