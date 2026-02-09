using Microsoft.AspNetCore.Mvc;
using TraderBot.Contracts.N8n;

namespace TraderBot.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(ILogger<AnalysisController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Receive analysis results from n8n
    /// </summary>
    [HttpPost("result")]
    public async Task<ActionResult> ReceiveAnalysisResult([FromBody] AnalysisResponse response, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Received analysis result from n8n: Symbol={Symbol}, Signal={Signal}, Confidence={Confidence}",
                response.Symbol, response.Signal, response.Confidence);

            // TODO: Process the analysis result
            // - Update internal state
            // - Trigger trading decisions based on signal
            // - Store analysis history
            
            await Task.CompletedTask; // Placeholder for async processing

            return Ok(new { Message = "Analysis result received successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing analysis result");
            return StatusCode(500, "Error processing analysis result");
        }
    }
}
