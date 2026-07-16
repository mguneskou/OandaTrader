using Microsoft.AspNetCore.SignalR;
using OandaTrader.Api.Realtime;

namespace OandaTrader.Api.Hubs;

/// <summary>
/// SignalR hub the React dashboard connects to for live prices, engine status, account
/// updates, and trade events. On connect, the current cached snapshot is replayed so a
/// freshly-loaded page isn't blank until the next push.
/// </summary>
public class EngineHub(EngineStateCache stateCache) : Hub<IEngineClient>
{
    public override async Task OnConnectedAsync()
    {
        var snapshot = stateCache.Snapshot();

        if (snapshot.EngineStatus is not null)
            await Clients.Caller.EngineStatus(snapshot.EngineStatus);

        if (snapshot.Account is not null)
            await Clients.Caller.Account(snapshot.Account);

        foreach (var price in snapshot.Prices.Values)
            await Clients.Caller.Price(price);

        await base.OnConnectedAsync();
    }
}
