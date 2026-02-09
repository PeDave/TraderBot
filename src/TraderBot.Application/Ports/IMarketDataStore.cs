using TraderBot.Domain.Entities;

namespace TraderBot.Application.Ports;

/// <summary>
/// Port for market data persistence
/// </summary>
public interface IMarketDataStore
{
    Task SaveCandleAsync(Candle candle, CancellationToken cancellationToken = default);
    Task<List<Candle>> GetCandlesAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Candle?> GetLatestCandleAsync(string symbol, CancellationToken cancellationToken = default);
}
