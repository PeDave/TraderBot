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

namespace TraderBot.Infrastructure.Exchanges.Bitget;

/// <summary>
/// Bitget exchange client adapter with real Bitget.Net integration
/// </summary>
public class BitgetExchangeClient : IExchangeClient
{
    private readonly ILogger<BitgetExchangeClient> _logger;
    private readonly ExchangeSettings _settings;
    private readonly BitgetRestClient _client;

    public BitgetExchangeClient(
        ILogger<BitgetExchangeClient> logger,
        ExchangeSettings settings)
    {
        _logger = logger;
        _settings = settings;

        // Initialize Bitget REST client
        _client = new BitgetRestClient(options =>
        {
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                options.ApiCredentials = new ApiCredentials(_settings.ApiKey, _settings.ApiSecret, _settings.Passphrase);
            }
        });
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
        _logger.LogInformation("Getting balance for asset: {Asset}", asset);
        
        try
        {
            var result = await _client.SpotApiV2.Account.GetSpotBalancesAsync(ct: cancellationToken);
            if (result.Success && result.Data != null)
            {
                var balance = result.Data.FirstOrDefault(b => b.Asset.Equals(asset, StringComparison.OrdinalIgnoreCase));
                var available = balance?.Available ?? 0;
                _logger.LogInformation("Balance for {Asset}: {Balance}", asset, available);
                return available;
            }
            else
            {
                _logger.LogError("Failed to get balance: {Error}", result.Error?.Message);
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for {Asset}", asset);
            return 0;
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
            // Determine if it's a Spot or Futures symbol
            bool isFutures = symbol.Contains("_UMCBL") || symbol.Contains("_DMCBL") || symbol.Contains("_CMCBL");
            
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
}
