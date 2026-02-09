using Bitget.Net.Clients;
using Bitget.Net.Enums;
using Bitget.Net.Enums.V2;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Enums;
using TraderBot.Domain.Settings;

namespace TraderBot.Infrastructure.Exchanges.Bitget;

/// <summary>
/// Bitget market data feed using WebSocket with real Bitget.Net integration
/// </summary>
public class BitgetMarketDataFeed : IMarketDataFeed
{
    private readonly ILogger<BitgetMarketDataFeed> _logger;
    private readonly ExchangeSettings _settings;
    private readonly BitgetSocketClient _socketClient;
    private UpdateSubscription? _spotSubscription;
    private UpdateSubscription? _futuresSubscription;
    private bool _isRunning;

    public BitgetMarketDataFeed(
        ILogger<BitgetMarketDataFeed> logger,
        ExchangeSettings settings)
    {
        _logger = logger;
        _settings = settings;
        
        // Initialize socket client with default options
        // The library handles auto-reconnection by default
        _socketClient = new BitgetSocketClient();
    }

    public event EventHandler<CandleReceivedEventArgs>? OnCandleReceived;

    public async Task StartAsync(string symbol, TimeFrame timeFrame, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Market data feed already running");
            return;
        }

        // Note: The symbol parameter is currently not used because this implementation
        // subscribes to both Spot (BTCUSDT) and Futures (BTCUSDT_UMCBL) simultaneously
        // as per requirements. Future versions could make this more flexible.
        _logger.LogInformation("Starting Bitget market data feed for Spot and Futures @ {TimeFrame}", timeFrame);
        
        try
        {
            var interval = MapTimeFrame(timeFrame);
            
            // Subscribe to Spot klines for BTCUSDT
            const string spotSymbol = "BTCUSDT";
            var spotResult = await _socketClient.SpotApiV2.SubscribeToKlineUpdatesAsync(
                spotSymbol,
                interval,
                data =>
                {
                    // Handle array of updates
                    foreach (var kline in data.Data)
                    {
                        _logger.LogDebug("Spot candle received: {Symbol} @ {Time}, Close: {Close}", 
                            spotSymbol, kline.OpenTime, kline.ClosePrice);
                        
                        OnCandleReceived?.Invoke(this, new CandleReceivedEventArgs
                        {
                            Symbol = spotSymbol,
                            Timestamp = kline.OpenTime,
                            Open = kline.OpenPrice,
                            High = kline.HighPrice,
                            Low = kline.LowPrice,
                            Close = kline.ClosePrice,
                            Volume = kline.Volume
                        });
                    }
                },
                cancellationToken);

            if (spotResult.Success)
            {
                _spotSubscription = spotResult.Data;
                _logger.LogInformation("Successfully subscribed to Spot kline updates for {Symbol}", spotSymbol);
            }
            else
            {
                _logger.LogError("Failed to subscribe to Spot kline updates. Success: {Success}, Error: {Error}, ErrorCode: {ErrorCode}", 
                    spotResult.Success, 
                    spotResult.Error?.Message ?? "null", 
                    spotResult.Error?.Code ?? 0);
            }

            // Subscribe to Futures klines for BTCUSDT_UMCBL
            const string futuresSymbol = "BTCUSDT";  // Symbol without _UMCBL suffix for V2 API
            const string futuresSymbolDisplay = "BTCUSDT_UMCBL";  // Display symbol with suffix
            var futuresResult = await _socketClient.FuturesApiV2.SubscribeToKlineUpdatesAsync(
                BitgetProductTypeV2.UsdtFutures,
                futuresSymbol,
                interval,
                data =>
                {
                    // Handle array of updates
                    foreach (var kline in data.Data)
                    {
                        _logger.LogDebug("Futures candle received: {Symbol} @ {Time}, Close: {Close}", 
                            futuresSymbolDisplay, kline.OpenTime, kline.ClosePrice);
                        
                        OnCandleReceived?.Invoke(this, new CandleReceivedEventArgs
                        {
                            Symbol = futuresSymbolDisplay,  // Use display symbol with suffix
                            Timestamp = kline.OpenTime,
                            Open = kline.OpenPrice,
                            High = kline.HighPrice,
                            Low = kline.LowPrice,
                            Close = kline.ClosePrice,
                            Volume = kline.Volume
                        });
                    }
                },
                cancellationToken);

            if (futuresResult.Success)
            {
                _futuresSubscription = futuresResult.Data;
                _logger.LogInformation("Successfully subscribed to Futures kline updates for {Symbol}", futuresSymbolDisplay);
            }
            else
            {
                _logger.LogError("Failed to subscribe to Futures kline updates. Success: {Success}, Error: {Error}, ErrorCode: {ErrorCode}", 
                    futuresResult.Success, 
                    futuresResult.Error?.Message ?? "null", 
                    futuresResult.Error?.Code ?? 0);
            }

            _isRunning = true;
            _logger.LogInformation("Bitget market data feed started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Bitget market data feed");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Market data feed not running");
            return;
        }

        _logger.LogInformation("Stopping Bitget market data feed");
        
        try
        {
            // Unsubscribe from Spot WebSocket
            if (_spotSubscription != null)
            {
                await _spotSubscription.CloseAsync();
                _spotSubscription = null;
                _logger.LogInformation("Unsubscribed from Spot kline updates");
            }

            // Unsubscribe from Futures WebSocket
            if (_futuresSubscription != null)
            {
                await _futuresSubscription.CloseAsync();
                _futuresSubscription = null;
                _logger.LogInformation("Unsubscribed from Futures kline updates");
            }

            _isRunning = false;
            _logger.LogInformation("Bitget market data feed stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Bitget market data feed");
            throw;
        }
    }

    /// <summary>
    /// Maps TimeFrame enum to Bitget stream KlineInterval
    /// </summary>
    private BitgetStreamKlineIntervalV2 MapTimeFrame(TimeFrame timeFrame)
    {
        return timeFrame switch
        {
            TimeFrame.OneMinute => BitgetStreamKlineIntervalV2.OneMinute,
            TimeFrame.FiveMinutes => BitgetStreamKlineIntervalV2.FiveMinutes,
            TimeFrame.FifteenMinutes => BitgetStreamKlineIntervalV2.FifteenMinutes,
            TimeFrame.ThirtyMinutes => BitgetStreamKlineIntervalV2.ThirtyMinutes,
            TimeFrame.OneHour => BitgetStreamKlineIntervalV2.OneHour,
            TimeFrame.FourHours => BitgetStreamKlineIntervalV2.FourHours,
            TimeFrame.OneDay => BitgetStreamKlineIntervalV2.OneDay,
            _ => throw new ArgumentException($"Unsupported timeframe: {timeFrame}")
        };
    }
}
