using Microsoft.EntityFrameworkCore;
using TraderBot.Application.Ports;
using TraderBot.Domain.Entities;

namespace TraderBot.Infrastructure.Persistence;

public class EfMarketDataStore : IMarketDataStore
{
    private readonly TradingDbContext _context;

    public EfMarketDataStore(TradingDbContext context)
    {
        _context = context;
    }

    public async Task SaveCandleAsync(Candle candle, CancellationToken cancellationToken = default)
    {
        // Check if candle already exists (avoid duplicates)
        var exists = await _context.Candles
            .AnyAsync(c => c.Symbol == candle.Symbol && c.Timestamp == candle.Timestamp, cancellationToken);

        if (!exists)
        {
            _context.Candles.Add(candle);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<Candle>> GetCandlesAsync(string symbol, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _context.Candles
            .Where(c => c.Symbol == symbol && c.Timestamp >= from && c.Timestamp <= to)
            .OrderBy(c => c.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<Candle?> GetLatestCandleAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await _context.Candles
            .Where(c => c.Symbol == symbol)
            .OrderByDescending(c => c.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
