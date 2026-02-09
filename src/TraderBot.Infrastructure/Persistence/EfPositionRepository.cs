using Microsoft.EntityFrameworkCore;
using TraderBot.Application.Ports;
using TraderBot.Domain.Entities;

namespace TraderBot.Infrastructure.Persistence;

public class EfPositionRepository : IPositionRepository
{
    private readonly TradingDbContext _context;

    public EfPositionRepository(TradingDbContext context)
    {
        _context = context;
    }

    public async Task<Position?> GetOpenPositionAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .Where(p => p.Symbol == symbol && p.IsOpen)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SavePositionAsync(Position position, CancellationToken cancellationToken = default)
    {
        if (position.Id == 0)
        {
            _context.Positions.Add(position);
        }
        else
        {
            _context.Positions.Update(position);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Position>> GetAllPositionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .OrderByDescending(p => p.OpenedAt)
            .ToListAsync(cancellationToken);
    }
}
