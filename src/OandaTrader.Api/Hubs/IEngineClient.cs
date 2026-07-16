using OandaTrader.Api.Realtime;

namespace OandaTrader.Api.Hubs;

/// <summary>Strongly-typed methods the server can invoke on connected browser clients.</summary>
public interface IEngineClient
{
    Task Price(PriceUpdate update);
    Task EngineStatus(EngineStatusUpdate update);
    Task Account(AccountUpdate update);
    Task Trade(TradeEvent tradeEvent);
    Task Log(EngineLogEntry entry);
}
