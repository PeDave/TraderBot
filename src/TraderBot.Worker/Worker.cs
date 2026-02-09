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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in TraderBot Worker");
            throw;
        }
        finally
        {
            _logger.LogInformation("Stopping bot...");
            await _orchestrator.StopAsync(CancellationToken.None);
        }
    }
}
