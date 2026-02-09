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
        try
        {
            // Try to add the candle
            _context.Candles.Add(candle);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
        {
            // Ignore duplicate candle (it already exists in the database)
            // This is expected behavior for real-time candle updates
            _context.Entry(candle).State = EntityState.Detached;
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
