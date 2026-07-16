using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using OandaTrader.Domain.Features;
using OandaTrader.Domain.Models;
using OandaTrader.Infrastructure.Data;
using OandaTrader.Infrastructure.Data.Entities;

namespace OandaTrader.Infrastructure.Ml;

public class TrainingResult
{
    public required bool Trained { get; init; }
    public string? Error { get; init; }
    public ModelVersion? Version { get; init; }
    public bool Promoted { get; init; }
}

/// <summary>
/// Trains the win-probability model from labeled trade outcomes (backtest-seeded + live),
/// evaluates it on a chronological hold-out slice, and promotes it to active only if it
/// isn't worse than the current active model on that same slice (see plan's retraining flow).
/// </summary>
public class ModelTrainingService(
    AppDbContext db,
    IOptions<MlOptions> options,
    ILogger<ModelTrainingService> logger)
{
    public async Task<TrainingResult> TrainAsync(CancellationToken ct = default)
    {
        var labeledTrades = (await db.Trades
                .Where(t => t.Outcome == TradeOutcomeResult.Win || t.Outcome == TradeOutcomeResult.Loss)
                .OrderBy(t => t.EntryTimeUtc)
                .ToListAsync(ct))
            // Re-running a backtest over an overlapping range re-inserts the same trades;
            // dedupe so repeated runs can't overweight duplicated samples in training.
            .DistinctBy(t => (t.Instrument, t.EntryTimeUtc, t.Direction, t.StrategySource))
            .ToList();

        var samples = new List<MlTradeSample>(labeledTrades.Count);
        foreach (var trade in labeledTrades)
        {
            FeatureVector? features;
            try
            {
                features = JsonSerializer.Deserialize<FeatureVector>(trade.FeaturesJson);
            }
            catch (JsonException)
            {
                continue; // skip malformed feature snapshots rather than failing the whole run
            }
            if (features is null) continue;

            samples.Add(MlTradeSample.FromFeatures(features, label: trade.Outcome == TradeOutcomeResult.Win));
        }

        if (samples.Count < options.Value.MinimumTrainingSamples)
        {
            return new TrainingResult
            {
                Trained = false,
                Error = $"Need at least {options.Value.MinimumTrainingSamples} labeled trades to train; have {samples.Count}. Run a backtest first to generate training data.",
            };
        }

        // Chronological split (samples are ordered by entry time): train on the earlier 80%,
        // evaluate on the most recent 20%, so evaluation never sees the "past" of its training data.
        int testCount = Math.Max(1, samples.Count / 5);
        var trainSamples = samples[..^testCount];
        var testSamples = samples[^testCount..];

        var ml = new MLContext(seed: 42);
        var trainData = ml.Data.LoadFromEnumerable(trainSamples);
        var testData = ml.Data.LoadFromEnumerable(testSamples);

        var pipeline = ml.Transforms.Concatenate("Features", MlTradeSample.FeatureColumnNames)
            .Append(ml.BinaryClassification.Trainers.FastTree(
                numberOfLeaves: 8,
                numberOfTrees: 100,
                minimumExampleCountPerLeaf: 5));

        var model = pipeline.Fit(trainData);

        var (newMetricsJson, newScore) = EvaluateSafely(ml, model, testData, testSamples);

        // Compare against the current active model on the same hold-out slice.
        var activeVersion = await db.ModelVersions.FirstOrDefaultAsync(v => v.IsActive, ct);
        bool promote = true;
        double? oldScore = null;
        if (activeVersion is not null)
        {
            var oldModelPath = ResolveModelPath(activeVersion.ModelFilePath);
            if (File.Exists(oldModelPath))
            {
                var oldModel = ml.Model.Load(oldModelPath, out _);
                (_, oldScore) = EvaluateSafely(ml, oldModel, testData, testSamples);
                promote = newScore is not null && (oldScore is null || newScore >= oldScore);
            }
        }

        var modelsDir = options.Value.ModelsDirectory;
        Directory.CreateDirectory(modelsDir);
        string fileName = $"model-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        ml.Model.Save(model, trainData.Schema, Path.Combine(modelsDir, fileName));

        var version = new ModelVersion
        {
            TrainedAtUtc = DateTime.UtcNow,
            TrainingSampleCount = samples.Count,
            MetricsJson = JsonSerializer.Serialize(new
            {
                metrics = JsonSerializer.Deserialize<JsonElement>(newMetricsJson),
                comparison = new { newScore, oldScore, promoted = promote },
                split = new { train = trainSamples.Count, test = testSamples.Count },
            }),
            IsActive = promote,
            ModelFilePath = fileName,
        };

        if (promote && activeVersion is not null)
        {
            activeVersion.IsActive = false;
        }

        db.ModelVersions.Add(version);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Trained model {File} on {Count} samples (promoted: {Promoted}, newScore: {New}, oldScore: {Old})",
            fileName, samples.Count, promote, newScore, oldScore);

        return new TrainingResult { Trained = true, Version = version, Promoted = promote };
    }

    /// <summary>Evaluates AUC/accuracy/F1 on the hold-out slice. AUC is undefined when the
    /// slice contains a single class (quite possible with few samples), so fall back to
    /// accuracy as the comparison score in that case.</summary>
    private static (string MetricsJson, double? Score) EvaluateSafely(
        MLContext ml, ITransformer model, IDataView testData, List<MlTradeSample> testSamples)
    {
        var predictions = model.Transform(testData);
        bool bothClassesPresent = testSamples.Any(s => s.Label) && testSamples.Any(s => !s.Label);

        try
        {
            var metrics = ml.BinaryClassification.Evaluate(predictions);
            var json = JsonSerializer.Serialize(new
            {
                auc = bothClassesPresent ? metrics.AreaUnderRocCurve : (double?)null,
                accuracy = metrics.Accuracy,
                f1 = metrics.F1Score,
            });
            return (json, bothClassesPresent ? metrics.AreaUnderRocCurve : metrics.Accuracy);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            return (JsonSerializer.Serialize(new { error = "evaluation failed: " + ex.Message }), null);
        }
    }

    public string ResolveModelPath(string fileName) => Path.Combine(options.Value.ModelsDirectory, fileName);
}
