using Bitget.Net.Clients;
using Bitget.Net.Enums;
using Bitget.Net.Enums.V2;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Logging;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Enums;
using TraderBot.Domain.Settings;
using DomainOrderSide = TraderBot.Domain.Enums.OrderSide;
using DomainOrderType = TraderBot.Domain.Enums.OrderType;
using BitgetOrderSide = Bitget.Net.Enums.V2.OrderSide;
using BitgetOrderType = Bitget.Net.Enums.V2.OrderType;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TraderBot.Infrastructure.Exchanges.Bitget;

/// <summary>
/// Bitget exchange client adapter with real Bitget.Net integration
/// </summary>
public class BitgetExchangeClient : IExchangeClient
{
    private readonly ILogger<BitgetExchangeClient> _logger;
    private readonly ExchangeSettings _settings;
    private readonly BitgetRestClient? _client;
    private readonly HttpClient _httpClient;
    private readonly bool _hasValidCredentials;

    public BitgetExchangeClient(
        ILogger<BitgetExchangeClient> logger,
        ExchangeSettings settings,
        HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings;
        _httpClient = httpClient;

        // Check if credentials are valid (not placeholder values)
        _hasValidCredentials = !string.IsNullOrEmpty(_settings.ApiKey) && 
            !string.IsNullOrEmpty(_settings.ApiSecret) &&
            !string.IsNullOrEmpty(_settings.Passphrase) &&
            !_settings.ApiKey.Contains("YOUR_") &&
            !_settings.ApiSecret.Contains("YOUR_") &&
            !_settings.Passphrase.Contains("YOUR_");

        // Only initialize Bitget REST client if credentials are valid
        if (_hasValidCredentials)
        {
            _client = new BitgetRestClient(options =>
            {
                options.ApiCredentials = new ApiCredentials(_settings.ApiKey, _settings.ApiSecret, _settings.Passphrase);
            });
        }
        else
        {
            _logger.LogWarning("Bitget client initialized without credentials. Only unauthenticated API calls will work.");
        }
    }

    public ExchangeType ExchangeType => ExchangeType.Bitget;

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing Bitget connection...");
        
        if (_client == null)
        {
            _logger.LogWarning("Cannot test connection: Bitget client not initialized (invalid credentials)");
            return false;
        }
        
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
        
        if (_client == null)
        {
            _logger.LogWarning("Cannot get balance: Bitget client not initialized (invalid credentials)");
            return 0;
        }
        
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
        
        if (_client == null)
        {
            _logger.LogError("Cannot place order: Bitget client not initialized (invalid credentials)");
            throw new InvalidOperationException("Bitget client not initialized. Please configure valid API credentials.");
        }
        
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
    /// Get all account balances from Bitget V2 API endpoint
    /// Uses GET /api/v2/account/all-account-balance
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetAllAccountBalancesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all account balances from Bitget V2 API");
        
        // Check if credentials are configured
        if (!_hasValidCredentials)
        {
            _logger.LogWarning("API credentials are not configured. Returning empty account balances.");
            return new Dictionary<string, decimal>();
        }
        
        try
        {
            const string endpoint = "/api/v2/account/all-account-balance";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var method = "GET";
            var queryString = "";
            var body = "";

            // Create signature for Bitget V2 API
            var signString = $"{timestamp}{method}{endpoint}{queryString}{body}";
            var signature = GenerateSignature(signString, _settings.ApiSecret);

            // Build request URL
            // Note: Bitget uses the same base URL for both testnet and production
            // The testnet uses a separate API key/secret pair for sandbox environment
            var baseUrl = "https://api.bitget.com";
            var requestUrl = $"{baseUrl}{endpoint}{queryString}";

            // Create HTTP request with Bitget V2 auth headers
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("ACCESS-KEY", _settings.ApiKey);
            request.Headers.Add("ACCESS-SIGN", signature);
            request.Headers.Add("ACCESS-PASSPHRASE", _settings.Passphrase);
            request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
            request.Headers.Add("locale", "en-US");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get all account balances. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseContent);
                throw new Exception($"Failed to get all account balances: {response.StatusCode}");
            }

            // Parse response
            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            // Check for error in response
            if (root.TryGetProperty("code", out var codeElement))
            {
                var code = codeElement.GetString();
                if (code != "00000")
                {
                    var message = root.TryGetProperty("msg", out var msgElement) 
                        ? msgElement.GetString() 
                        : "Unknown error";
                    _logger.LogError("Bitget API error - Code: {Code}, Message: {Message}", code, message);
                    throw new Exception($"Bitget API error: Code={code}, Message={message}");
                }
            }

            // Parse account balances
            var accountBalances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            
            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var accountElement in dataElement.EnumerateArray())
                {
                    if (accountElement.TryGetProperty("accountType", out var typeElement) &&
                        accountElement.TryGetProperty("usdtBalance", out var balanceElement))
                    {
                        var accountType = typeElement.GetString() ?? "unknown";
                        var usdtBalance = decimal.TryParse(balanceElement.GetString(), out var balance) 
                            ? balance 
                            : 0m;
                        
                        accountBalances[accountType] = usdtBalance;
                        _logger.LogDebug("Account balance - Type: {Type}, USDT: {Balance}", accountType, usdtBalance);
                    }
                }
            }

            _logger.LogInformation("Retrieved balances for {Count} account types", accountBalances.Count);
            return accountBalances;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all account balances");
            throw;
        }
    }

    /// <summary>
    /// Generate HMAC SHA256 signature for Bitget V2 API
    /// </summary>
    private static string GenerateSignature(string message, string secret)
    {
        var encoding = new UTF8Encoding();
        var keyBytes = encoding.GetBytes(secret);
        var messageBytes = encoding.GetBytes(message);
        
        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
