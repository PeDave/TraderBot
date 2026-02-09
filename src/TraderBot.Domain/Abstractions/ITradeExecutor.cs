using TraderBot.Domain.Enums;

namespace TraderBot.Domain.Abstractions;

/// <summary>
/// Interface for executing trades with risk management
/// </summary>
public interface ITradeExecutor
{
    Task<string> ExecuteTradeAsync(
        string symbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
