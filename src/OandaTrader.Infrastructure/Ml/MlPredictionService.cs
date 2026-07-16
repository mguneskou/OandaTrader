using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using OandaTrader.Domain.Features;
using OandaTrader.Domain.Strategies;
using OandaTrader.Infrastructure.Data;

namespace OandaTrader.Infrastructure.Ml;

/// <summary>
/// Singleton that loads the active ModelVersion's .zip and scores feature vectors.
/// PredictionEngine isn't thread-safe, so scoring is serialized behind a lock — trade
/// decisions happen at candle-close cadence, so contention is negligible.
/// Call <see cref="ReloadActiveModel"/> after training promotes a new version.
/// </summary>
public class MlPredictionService(
    IServiceScopeFactory scopeFactory,
    IOptions<MlOptions> options,
    ILogger<MlPredictionService> logger) : IWinProbabilityPredictor
{
    private readonly Lock _lock = new();
    private readonly MLContext _ml = new();
    private PredictionEngine<MlTradeSample, MlTradePrediction>? _engine;
    private bool _loadAttempted;

    public bool IsReady
    {
        get
        {
            EnsureLoaded();
            lock (_lock) return _engine is not null;
        }
    }

    public double PredictWinProbability(FeatureVector features)
    {
        EnsureLoaded();
        lock (_lock)
        {
            if (_engine is null)
            {
                throw new InvalidOperationException("No active trained model is loaded.");
            }
            var prediction = _engine.Predict(MlTradeSample.FromFeatures(features));
            return prediction.Probability;
        }
    }

    public void ReloadActiveModel()
    {
        lock (_lock)
        {
            _loadAttempted = false;
            _engine = null;
        }
        EnsureLoaded();
    }

    private void EnsureLoaded()
    {
        lock (_lock)
        {
            if (_loadAttempted) return;
            _loadAttempted = true;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var active = db.ModelVersions.AsNoTracking().FirstOrDefault(v => v.IsActive);
            if (active is null)
            {
                logger.LogInformation("No active model version in the database; ML predictions unavailable until one is trained.");
                return;
            }

            var path = Path.Combine(options.Value.ModelsDirectory, active.ModelFilePath);
            if (!File.Exists(path))
            {
                logger.LogWarning("Active model version {Id} points to missing file {Path}.", active.Id, path);
                return;
            }

            var model = _ml.Model.Load(path, out _);
            _engine = _ml.Model.CreatePredictionEngine<MlTradeSample, MlTradePrediction>(model);
            logger.LogInformation("Loaded active model version {Id} from {Path}.", active.Id, path);
        }
    }
}
