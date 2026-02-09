using TraderBot.Application.Ports;
using TraderBot.Domain.Abstractions;

namespace TraderBot.Infrastructure.Services;

/// <summary>
/// Wallet service that queries exchange for balance information
/// </summary>
public class WalletService : IWalletService
{
    private readonly IExchangeClient _exchangeClient;

    public WalletService(IExchangeClient exchangeClient)
    {
        _exchangeClient = exchangeClient;
    }

    public async Task<decimal> GetAvailableBalanceAsync(string asset, CancellationToken cancellationToken = default)
    {
        return await _exchangeClient.GetBalanceAsync(asset, cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetAllBalancesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement getting all balances
        // For now, return common trading assets
        var balances = new Dictionary<string, decimal>
        {
            { "USDT", await _exchangeClient.GetBalanceAsync("USDT", cancellationToken) },
            { "BTC", await _exchangeClient.GetBalanceAsync("BTC", cancellationToken) },
            { "ETH", await _exchangeClient.GetBalanceAsync("ETH", cancellationToken) }
        };

        return balances;
    }
}
