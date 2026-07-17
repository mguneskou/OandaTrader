using Microsoft.AspNetCore.SignalR;
using OandaTrader.Api.Hubs;

namespace OandaTrader.Api.Realtime;

/// <summary>Single entry point the hosted services use to push updates to all connected
/// dashboard clients. Also updates <see cref="EngineStateCache"/> so late-joining clients get
/// the current snapshot on connect.</summary>
public class EngineBroadcaster(IHubContext<EngineHub, IEngineClient> hub, EngineStateCache stateCache)
{
    public Task BroadcastPriceAsync(PriceUpdate update)
    {
        stateCache.SetPrice(update);
        return hub.Clients.All.Price(update);
    }

    public Task BroadcastEngineStatusAsync(EngineStatusUpdate update)
    {
        stateCache.SetEngineStatus(update);
        return hub.Clients.All.EngineStatus(update);
    }

    public Task BroadcastAccountAsync(AccountUpdate update)
    {
        stateCache.SetAccount(update);
        return hub.Clients.All.Account(update);
    }

    public Task BroadcastTradeAsync(TradeEvent tradeEvent) => hub.Clients.All.Trade(tradeEvent);

    public Task BroadcastLogAsync(EngineLogEntry entry) => hub.Clients.All.Log(entry);

    // Not cached: backtest progress is transient and a page joining mid-run doesn't need
    // history replayed, unlike price/status/account.
    public Task BroadcastBacktestProgressAsync(BacktestProgressUpdate update) =>
        hub.Clients.All.BacktestProgress(update);
}
