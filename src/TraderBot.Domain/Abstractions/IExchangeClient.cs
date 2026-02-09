using TraderBot.Domain.Enums;

namespace TraderBot.Domain.Abstractions;

/// <summary>
/// Core interface for exchange client implementations
/// </summary>
public interface IExchangeClient
{
    ExchangeType ExchangeType { get; }
    
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    Task<decimal> GetBalanceAsync(string asset, CancellationToken cancellationToken = default);
    
    Task<string> PlaceOrderAsync(
        string symbol,
        OrderSide side,
        OrderType type,
        decimal quantity,
        decimal? price = null,
        CancellationToken cancellationToken = default);
}
