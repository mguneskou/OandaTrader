using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OandaTrader.Infrastructure.Oanda;

namespace OandaTrader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(OandaRestClient oanda, IOptions<OandaOptions> oandaOptions) : ControllerBase
{
    /// <summary>
    /// Fetches the Oanda account summary. Also serves as the "Test Connection" check:
    /// success means the configured API token + account ID are valid.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var options = oandaOptions.Value;
        if (!options.IsConfigured)
        {
            return BadRequest(new
            {
                error = "Oanda API token and/or account ID are not configured. " +
                         "Set Oanda:ApiToken and Oanda:AccountId via .NET user-secrets."
            });
        }

        try
        {
            var summary = await oanda.GetAccountSummaryAsync(options.AccountId, ct);
            return Ok(summary);
        }
        catch (OandaApiException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}
