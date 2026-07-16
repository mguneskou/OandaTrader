namespace OandaTrader.Infrastructure.Data.Entities;

public class ModelVersion
{
    public int Id { get; set; }
    public DateTime TrainedAtUtc { get; set; }
    public int TrainingSampleCount { get; set; }

    /// <summary>JSON blob of training metrics (e.g. accuracy, AUC on the held-out slice).</summary>
    public string MetricsJson { get; set; } = "{}";

    public bool IsActive { get; set; }

    /// <summary>Path to the serialized ML.NET model (.zip), relative to App_Data/Models.</summary>
    public string ModelFilePath { get; set; } = "";
}
