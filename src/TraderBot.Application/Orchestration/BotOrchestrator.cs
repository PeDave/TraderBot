using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraderBot.Application.Ports;
using TraderBot.Application.Strategies;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Entities;
using TraderBot.Domain.Enums;
using TraderBot.Domain.Settings;

namespace TraderBot.Application.Orchestration;

/// <summary>
/// Main bot orchestrator that coordinates market data feed and strategy execution
/// </summary>
public class BotOrchestrator
{
    private readonly ILogger<BotOrchestrator> _logger;
    private readonly BotSettings _settings;
    private readonly IMarketDataFeed _marketDataFeed;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    private BotStatus _status = BotStatus.Stopped;

    public BotOrchestrator(
        ILogger<BotOrchestrator> logger,
        BotSettings settings,
        IMarketDataFeed marketDataFeed,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _settings = settings;
        _marketDataFeed = marketDataFeed;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public BotStatus Status => _status;

    /// <summary>
    /// Starts the trading bot
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_status == BotStatus.Running)
        {
            _logger.LogWarning("Bot is already running");
            return;
        }

        _logger.LogInformation("Starting bot for symbol: {Symbol}, TimeFrame: {TimeFrame}", 
            _settings.Symbol, _settings.TimeFrame);
        
        _status = BotStatus.Starting;

        try
        {
            // Subscribe to candle events
            _marketDataFeed.OnCandleReceived += OnCandleReceived;
            
            // Start market data feed
            await _marketDataFeed.StartAsync(_settings.Symbol, _settings.TimeFrame, cancellationToken);
            
            _status = BotStatus.Running;
            _logger.LogInformation("Bot started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start bot");
            _status = BotStatus.Error;
            throw;
        }
    }

    /// <summary>
    /// Stops the trading bot
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_status == BotStatus.Stopped)
        {
            _logger.LogWarning("Bot is already stopped");
            return;
        }

        _logger.LogInformation("Stopping bot");
        
        try
        {
            // Unsubscribe from candle events
            _marketDataFeed.OnCandleReceived -= OnCandleReceived;
            
            // Stop market data feed
            await _marketDataFeed.StopAsync(cancellationToken);
            
            _status = BotStatus.Stopped;
            _logger.LogInformation("Bot stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping bot");
            throw;
        }
    }

    /// <summary>
    /// Event handler for when a new candle is received
    /// Queues work asynchronously to avoid blocking the event source
    /// </summary>
    private void OnCandleReceived(object? sender, CandleReceivedEventArgs e)
    {
        // Queue work to avoid blocking the event source
        // Using fire-and-forget pattern with proper exception handling
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Received candle: {Symbol} @ {Timestamp}, Close: {Close}", 
                    e.Symbol, e.Timestamp, e.Close);

                // Create a scope for scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                var marketDataStore = scope.ServiceProvider.GetRequiredService<IMarketDataStore>();
                var strategy = scope.ServiceProvider.GetRequiredService<MartingaleStrategy>();

                // Store candle in database
                var candle = new Candle
                {
                    Symbol = e.Symbol,
                    Timestamp = e.Timestamp,
                    Open = e.Open,
                    High = e.High,
                    Low = e.Low,
                    Close = e.Close,
                    Volume = e.Volume,
                    TimeFrame = _settings.TimeFrame.ToString()
                };

                await marketDataStore.SaveCandleAsync(candle);

                // Execute strategy
                await strategy.OnCandleAsync(candle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing candle");
            }
        });
    }
}
