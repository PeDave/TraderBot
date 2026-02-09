using TraderBot.Domain.Entities;

namespace TraderBot.Application.Ports;

/// <summary>
/// Port for position management
/// </summary>
public interface IPositionRepository
{
    Task<Position?> GetOpenPositionAsync(string symbol, CancellationToken cancellationToken = default);
    Task SavePositionAsync(Position position, CancellationToken cancellationToken = default);
    Task<List<Position>> GetAllPositionsAsync(CancellationToken cancellationToken = default);
}
