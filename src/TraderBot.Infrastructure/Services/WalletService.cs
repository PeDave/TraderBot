using Microsoft.Extensions.Logging;
using TraderBot.Application.Ports;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Settings;

namespace TraderBot.Infrastructure.Services;

/// <summary>
/// Wallet service that queries exchange for balance information
/// </summary>
public class WalletService : IWalletService
{
    private readonly IExchangeClient _exchangeClient;
    private readonly ILogger<WalletService> _logger;
    private readonly TradingSettings _tradingSettings;

    public WalletService(
        IExchangeClient exchangeClient, 
        ILogger<WalletService> logger,
        TradingSettings tradingSettings)
    {
        _exchangeClient = exchangeClient;
        _logger = logger;
        _tradingSettings = tradingSettings;
    }

    public async Task<decimal> GetAvailableBalanceAsync(string asset, CancellationToken cancellationToken = default)
    {
        // If trading is disabled, return 0 without querying (market-data-only mode)
        if (!_tradingSettings.Enabled)
        {
            _logger.LogDebug("Trading disabled - returning 0 balance for {Asset}", asset);
            return 0;
        }

        // If balance check is not required, return 0 without failing
        if (!_tradingSettings.RequireBalanceCheck)
        {
            _logger.LogDebug("Balance check not required - returning 0 for {Asset}", asset);
            return 0;
        }

        // Retry logic with exponential backoff
        int attempt = 0;
        int delay = _tradingSettings.RetryDelaySeconds;

        while (attempt < _tradingSettings.MaxRetries)
        {
            try
            {
                var balance = await _exchangeClient.GetBalanceAsync(asset, cancellationToken);
                return balance;
            }
            catch (Exception ex)
            {
                attempt++;
                
                if (attempt >= _tradingSettings.MaxRetries)
                {
                    _logger.LogError(ex, "Failed to get balance for {Asset} after {Attempts} attempts", asset, attempt);
                    
                    // If balance check is required, throw the exception
                    if (_tradingSettings.RequireBalanceCheck)
                    {
                        throw;
                    }
                    
                    // Otherwise, return 0 as fallback
                    _logger.LogWarning("Returning 0 balance as fallback for {Asset}", asset);
                    return 0;
                }

                _logger.LogWarning(ex, "Balance request failed for {Asset} (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s", 
                    asset, attempt, _tradingSettings.MaxRetries, delay);
                
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                
                // Exponential backoff
                if (_tradingSettings.UseExponentialBackoff)
                {
                    delay *= 2;
                }
            }
        }

        // Should not reach here, but return 0 as fallback
        return 0;
    }

    public async Task<Dictionary<string, decimal>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        // If trading is disabled, return empty dictionary
        if (!_tradingSettings.Enabled)
        {
            _logger.LogDebug("Trading disabled - returning empty balances");
            return new Dictionary<string, decimal>();
        }

        // TODO: Implement getting all balances
        // For now, return common trading assets with error handling
        var balances = new Dictionary<string, decimal>();
        var assets = new[] { "USDT", "BTC", "ETH" };

        foreach (var asset in assets)
        {
            try
            {
                balances[asset] = await GetAvailableBalanceAsync(asset, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get balance for {Asset}, setting to 0", asset);
                balances[asset] = 0;
            }
        }

        return balances;
    }
}
