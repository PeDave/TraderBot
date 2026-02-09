namespace TraderBot.Application.Ports;

/// <summary>
/// Port for wallet/balance management
/// </summary>
public interface IWalletService
{
    Task<decimal> GetAvailableBalanceAsync(string asset, CancellationToken cancellationToken = default);
    Task<Dictionary<string, decimal>> GetAllBalancesAsync(CancellationToken cancellationToken = default);
}
