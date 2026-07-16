namespace OandaTrader.Infrastructure.Ml;

public class MlOptions
{
    public const string SectionName = "Ml";

    /// <summary>Directory trained model .zip files are stored in. Relative paths resolve
    /// against the API's content root (set at startup).</summary>
    public string ModelsDirectory { get; set; } = "App_Data/Models";

    /// <summary>Minimum labeled trades required before a first model can be trained.</summary>
    public int MinimumTrainingSamples { get; set; } = 50;
}
