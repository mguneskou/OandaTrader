namespace OandaTrader.Infrastructure.Oanda;

public class OandaOptions
{
    public const string SectionName = "Oanda";

    public string ApiToken { get; set; } = "";
    public string AccountId { get; set; } = "";

    /// <summary>"Practice" (demo) or "Live". This app is built for Practice only.</summary>
    public string Environment { get; set; } = "Practice";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiToken) && !string.IsNullOrWhiteSpace(AccountId);

    public Uri RestBaseUri => new(Environment == "Live"
        ? "https://api-fxtrade.oanda.com"
        : "https://api-fxpractice.oanda.com");

    public Uri StreamingBaseUri => new(Environment == "Live"
        ? "https://stream-fxtrade.oanda.com"
        : "https://stream-fxpractice.oanda.com");
}
