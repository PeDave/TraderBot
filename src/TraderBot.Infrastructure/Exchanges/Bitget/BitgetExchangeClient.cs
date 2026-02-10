using Bitget.Net.Clients;
using Bitget.Net.Enums;
using Bitget.Net.Enums.V2;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using TraderBot.Contracts.DTOs;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Enums;
using TraderBot.Domain.Settings;
using TraderBot.Infrastructure.Exchanges.Bitget.Models;
using DomainOrderSide = TraderBot.Domain.Enums.OrderSide;
using DomainOrderType = TraderBot.Domain.Enums.OrderType;
using BitgetOrderSide = Bitget.Net.Enums.V2.OrderSide;
using BitgetOrderType = Bitget.Net.Enums.V2.OrderType;

namespace TraderBot.Infrastructure.Exchanges.Bitget;

/// <summary>
/// Bitget exchange client adapter with real Bitget.Net integration
/// </summary>
public class BitgetExchangeClient : IExchangeClient
{
    private readonly ILogger<BitgetExchangeClient> _logger;
    private readonly ExchangeSettings _settings;
    private readonly BitgetRestClient _client;
    private readonly BitgetV2RestClient _v2Client;

    public BitgetExchangeClient(
        ILogger<BitgetExchangeClient> logger,
        ExchangeSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _settings = settings;

        // Initialize Bitget REST client
        _client = new BitgetRestClient(options =>
        {
            // Only set credentials if they are properly configured
            if (!string.IsNullOrEmpty(_settings.ApiKey) && 
                !string.IsNullOrEmpty(_settings.ApiSecret) &&
                !string.IsNullOrEmpty(_settings.Passphrase) &&
                !_settings.Passphrase.Equals("YOUR_PASSPHRASE_HERE", StringComparison.OrdinalIgnoreCase))
            {
                options.ApiCredentials = new ApiCredentials(_settings.ApiKey, _settings.ApiSecret, _settings.Passphrase);
            }
        });

        // Initialize V2 REST client for custom endpoints
        var httpClient = httpClientFactory.CreateClient();
        _v2Client = new BitgetV2RestClient(httpClient, settings);
    }

    public ExchangeType ExchangeType => ExchangeType.Bitget;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing Bitget connection...");
        
        try
        {
            var result = await _client.SpotApiV2.ExchangeData.GetServerTimeAsync(cancellationToken);
            if (result.Success)
            {
                _logger.LogInformation("Bitget connection test successful. Server time: {Time}", result.Data);
                return true;
            }
            else
            {
                _logger.LogError("Bitget connection test failed: {Error}", result.Error?.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Bitget connection");
            return false;
        }
    }

    public async Task<decimal> GetBalanceAsync(string asset, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting {AccountType} balance for asset: {Asset}", _settings.AccountType, asset);
        
        try
        {
            // Support both spot and futures account types
            bool isFutures = _settings.AccountType.Equals("futures", StringComparison.OrdinalIgnoreCase);
            
            if (isFutures)
            {
                // Futures account balance query is not fully implemented in the current Bitget.Net library version
                // The library may not have the required API endpoint available
                _logger.LogWarning("Futures account balance query is not fully implemented in the current library version. " +
                    "Returning 0 for {Asset}. For production use with futures, please verify the library supports the required API endpoints, " +
                    "or use AccountType=spot if spot trading is suitable for your needs.", asset);
                return 0;
            }
            else
            {
                // Get spot account balance
                var spotResult = await _client.SpotApiV2.Account.GetSpotBalancesAsync(ct: cancellationToken);
                
                if (spotResult.Success && spotResult.Data != null)
                {
                    var balance = spotResult.Data.FirstOrDefault(b => b.Asset.Equals(asset, StringComparison.OrdinalIgnoreCase));
                    var available = balance?.Available ?? 0;
                    _logger.LogInformation("Spot balance for {Asset}: {Balance}", asset, available);
                    return available;
                }
                else
                {
                    var errorCode = spotResult.Error?.Code?.ToString() ?? "Unknown";
                    var errorMessage = spotResult.Error?.Message ?? "Unknown";
                    _logger.LogError("Failed to get spot balance - Error Code: {ErrorCode}, Message: {ErrorMessage}", 
                        errorCode, errorMessage);
                    throw new Exception($"Failed to get spot balance: Code={errorCode}, Message={errorMessage}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting {AccountType} balance for {Asset}", _settings.AccountType, asset);
            throw;
        }
    }

    public async Task<string> PlaceOrderAsync(
        string symbol,
        DomainOrderSide side,
        DomainOrderType type,
        decimal quantity,
        decimal? price = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Placing {Type} {Side} order for {Symbol}: Quantity={Quantity}, Price={Price}",
            type, side, symbol, quantity, price);
        
        try
        {
            // Determine if it's a Spot or Futures symbol based on common Bitget suffixes
            // Futures symbols typically end with _UMCBL (USDT-margined), _DMCBL (coin-margined), or _CMCBL
            bool isFutures = symbol.EndsWith("_UMCBL", StringComparison.OrdinalIgnoreCase) || 
                           symbol.EndsWith("_DMCBL", StringComparison.OrdinalIgnoreCase) || 
                           symbol.EndsWith("_CMCBL", StringComparison.OrdinalIgnoreCase);
            
            var bitgetSide = side == DomainOrderSide.Buy 
                ? BitgetOrderSide.Buy 
                : BitgetOrderSide.Sell;
            
            var bitgetType = type == DomainOrderType.Market 
                ? BitgetOrderType.Market 
                : BitgetOrderType.Limit;

            if (isFutures)
            {
                // Place Futures order (UMCBL - USDT-margined)
                // For market orders on futures, we need to use different parameters
                var result = await _client.FuturesApiV2.Trading.PlaceOrderAsync(
                    BitgetProductTypeV2.UsdtFutures,
                    symbol: symbol,
                    marginAsset: "USDT",  // USDT-margined futures
                    side: bitgetSide,
                    type: bitgetType,
                    marginMode: MarginMode.CrossMargin,  // Use cross margin by default
                    quantity: quantity,
                    price: price,
                    timeInForce: bitgetType == BitgetOrderType.Market 
                        ? TimeInForce.ImmediateOrCancel 
                        : TimeInForce.GoodTillCanceled,
                    ct: cancellationToken);

                if (result.Success && result.Data != null)
                {
                    var orderId = result.Data.OrderId;
                    _logger.LogInformation("Futures order placed successfully. Order ID: {OrderId}", orderId);
                    return orderId;
                }
                else
                {
                    _logger.LogError("Failed to place futures order: {Error}", result.Error?.Message);
                    throw new Exception($"Failed to place futures order: {result.Error?.Message}");
                }
            }
            else
            {
                // Place Spot order
                var result = await _client.SpotApiV2.Trading.PlaceOrderAsync(
                    symbol: symbol,
                    side: bitgetSide,
                    type: bitgetType,
                    quantity: quantity,
                    timeInForce: bitgetType == BitgetOrderType.Market 
                        ? TimeInForce.ImmediateOrCancel 
                        : TimeInForce.GoodTillCanceled,
                    price: price,
                    ct: cancellationToken);

                if (result.Success && result.Data != null)
                {
                    var orderId = result.Data.OrderId;
                    _logger.LogInformation("Spot order placed successfully. Order ID: {OrderId}", orderId);
                    return orderId;
                }
                else
                {
                    _logger.LogError("Failed to place spot order: {Error}", result.Error?.Message);
                    throw new Exception($"Failed to place spot order: {result.Error?.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order for {Symbol}", symbol);
            throw;
        }
    }

    /// <summary>
    /// Get account summary with balances across all account types
    /// Implements Bitget V2 API endpoint: GET /api/v2/account/all-account-balance
    /// </summary>
    public async Task<AccountSummaryDto> GetAccountSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting account summary from Bitget V2 API");

        try
        {
            var response = await _v2Client.GetAsync<BitgetV2Response<BitgetAccountBalanceList>>(
                "/api/v2/account/all-account-balance",
                cancellationToken);

            if (response == null)
            {
                _logger.LogError("Received null response from Bitget V2 API");
                return new AccountSummaryDto();
            }

            if (!response.IsSuccess)
            {
                _logger.LogError("Bitget V2 API returned error - Code: {Code}, Message: {Message}",
                    response.Code, response.Message);
                return new AccountSummaryDto();
            }

            if (response.Data == null)
            {
                _logger.LogWarning("Bitget V2 API returned success but no data");
                return new AccountSummaryDto();
            }

            // Parse and transform the response
            var summary = new AccountSummaryDto();

            // Parse total USDT
            if (decimal.TryParse(response.Data.TotalUSDT, out var totalUsdt))
            {
                summary.TotalUsdt = totalUsdt;
            }

            // Parse account balances
            foreach (var account in response.Data.AccountList)
            {
                if (decimal.TryParse(account.UsdtBalance, out var usdtBalance))
                {
                    var accountBalance = new AccountBalanceDto
                    {
                        AccountType = account.AccountType,
                        UsdtBalance = usdtBalance
                    };

                    summary.Balances.Add(accountBalance);
                    summary.BalancesByType[account.AccountType] = usdtBalance;

                    // Set convenience properties for common account types
                    var accountTypeLower = account.AccountType.ToLowerInvariant();
                    switch (accountTypeLower)
                    {
                        case "spot":
                            summary.SpotBalance = usdtBalance;
                            break;
                        case "futures":
                        case "mix":
                            summary.FuturesBalance = usdtBalance;
                            break;
                        case "funding":
                            summary.FundingBalance = usdtBalance;
                            break;
                        case "earn":
                            summary.EarnBalance = usdtBalance;
                            break;
                        case "bots":
                            summary.BotsBalance = usdtBalance;
                            break;
                        case "margin":
                        case "cross_margin":
                        case "isolated_margin":
                            summary.MarginBalance += usdtBalance; // Aggregate margin types
                            break;
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to parse USDT balance for account type {AccountType}: {Balance}",
                        account.AccountType, account.UsdtBalance);
                }
            }

            _logger.LogInformation("Successfully retrieved account summary. Total USDT: {Total}, Account types: {Count}",
                summary.TotalUsdt, summary.Balances.Count);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account summary from Bitget V2 API");
            // Return empty summary on error for graceful degradation
            return new AccountSummaryDto();
        }
    }
}
