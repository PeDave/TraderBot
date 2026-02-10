using Microsoft.Extensions.Logging;
using TraderBot.Application.Ports;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Settings;
using TraderBot.Infrastructure.Exchanges.Bitget;

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

        // Safety fallback - should not be reached under normal circumstances
        // The loop exits via return or throw in all expected code paths
        _logger.LogWarning("Unexpected exit from retry loop for {Asset}, returning 0", asset);
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

    public async Task<Dictionary<string, decimal>> GetAccountSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting account balance summary");
        
        // If trading is disabled and balance check is not required, return empty dictionary
        if (!_tradingSettings.Enabled && !_tradingSettings.RequireBalanceCheck)
        {
            _logger.LogDebug("Trading disabled and balance check not required - returning empty account summary");
            return new Dictionary<string, decimal>();
        }

        // Retry logic with exponential backoff
        int attempt = 0;
        int delay = _tradingSettings.RetryDelaySeconds;

        while (attempt < _tradingSettings.MaxRetries)
        {
            try
            {
                // Cast to BitgetExchangeClient to access GetAllAccountBalancesAsync
                if (_exchangeClient is BitgetExchangeClient bitgetClient)
                {
                    var accountBalances = await bitgetClient.GetAllAccountBalancesAsync(cancellationToken);
                    _logger.LogInformation("Successfully retrieved account summary with {Count} account types", 
                        accountBalances.Count);
                    return accountBalances;
                }
                else
                {
                    _logger.LogWarning("Exchange client does not support GetAllAccountBalancesAsync");
                    return new Dictionary<string, decimal>();
                }
            }
            catch (Exception ex)
            {
                attempt++;
                
                if (attempt >= _tradingSettings.MaxRetries)
                {
                    _logger.LogError(ex, "Failed to get account summary after {Attempts} attempts", attempt);
                    
                    // If balance check is required, throw the exception
                    if (_tradingSettings.RequireBalanceCheck)
                    {
                        throw;
                    }
                    
                    // Otherwise, return empty dictionary as fallback
                    _logger.LogWarning("Returning empty account summary as fallback");
                    return new Dictionary<string, decimal>();
                }

                _logger.LogWarning(ex, "Account summary request failed (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s", 
                    attempt, _tradingSettings.MaxRetries, delay);
                
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                
                // Exponential backoff
                if (_tradingSettings.UseExponentialBackoff)
                {
                    delay *= 2;
                }
            }
        }

        // Safety fallback
        _logger.LogWarning("Unexpected exit from retry loop for account summary, returning empty dictionary");
        return new Dictionary<string, decimal>();
    }
}
