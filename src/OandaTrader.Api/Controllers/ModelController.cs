using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OandaTrader.Infrastructure.Data;
using OandaTrader.Infrastructure.Ml;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelController(
    ModelTrainingService trainer,
    MlPredictionService predictor,
    AppDbContext db) : ControllerBase
{
    [HttpPost("train")]
    public async Task<IActionResult> Train(CancellationToken ct)
    {
        var result = await trainer.TrainAsync(ct);
        if (!result.Trained)
        {
            return BadRequest(new { error = result.Error });
        }

        if (result.Promoted)
        {
            predictor.ReloadActiveModel();
        }

        return Ok(new
        {
            result.Version!.Id,
            result.Version.TrainedAtUtc,
            result.Version.TrainingSampleCount,
            result.Promoted,
            Metrics = result.Version.MetricsJson,
        });
    }

    [HttpGet("versions")]
    public async Task<IActionResult> GetVersions(CancellationToken ct)
    {
        var versions = await db.ModelVersions.OrderByDescending(v => v.TrainedAtUtc).ToListAsync(ct);
        return Ok(versions);
    }

    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(new { ready = predictor.IsReady });
}
