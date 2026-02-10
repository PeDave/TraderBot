using TraderBot.Application.Orchestration;

namespace TraderBot.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly BotOrchestrator _orchestrator;

    public Worker(ILogger<Worker> logger, BotOrchestrator orchestrator)
    {
        _logger = logger;
        _orchestrator = orchestrator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TraderBot Worker starting...");

        try
        {
            // Start the bot orchestrator
            await _orchestrator.StartAsync(stoppingToken);

            // Keep the worker running while the bot is active
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Bot Status: {Status}", _orchestrator.Status);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // This is expected when stopping the service
            _logger.LogInformation("TraderBot Worker stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TraderBot Worker - attempting graceful recovery");
            
            // Don't throw - let the worker continue running for market data collection
            // The orchestrator will handle its own state
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TraderBot Worker stopping during error recovery");
            }
        }
        finally
        {
            _logger.LogInformation("Stopping bot...");
            try
            {
                await _orchestrator.StopAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping orchestrator");
            }
        }
    }
}
