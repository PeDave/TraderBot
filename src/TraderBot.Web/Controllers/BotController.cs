using Microsoft.AspNetCore.Mvc;
using TraderBot.Application.Orchestration;
using TraderBot.Domain.Enums;

namespace TraderBot.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BotController : ControllerBase
{
    private readonly BotOrchestrator _orchestrator;
    private readonly ILogger<BotController> _logger;

    public BotController(BotOrchestrator orchestrator, ILogger<BotController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get current bot status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new { Status = _orchestrator.Status.ToString() });
    }

    /// <summary>
    /// Start the trading bot
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult> StartBot(CancellationToken cancellationToken)
    {
        try
        {
            await _orchestrator.StartAsync(cancellationToken);
            return Ok(new { Message = "Bot started successfully", Status = _orchestrator.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting bot");
            return StatusCode(500, "Error starting bot");
        }
    }

    /// <summary>
    /// Stop the trading bot
    /// </summary>
    [HttpPost("stop")]
    public async Task<ActionResult> StopBot(CancellationToken cancellationToken)
    {
        try
        {
            await _orchestrator.StopAsync(cancellationToken);
            return Ok(new { Message = "Bot stopped successfully", Status = _orchestrator.Status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping bot");
            return StatusCode(500, "Error stopping bot");
        }
    }
}
