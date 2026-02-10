namespace TraderBot.Contracts.DTOs;

/// <summary>
/// Account balance summary data transfer object
/// Represents the Bitget V2 all-account-balance response
/// </summary>
public class AccountBalanceSummaryDto
{
    /// <summary>
    /// Dictionary of account types to their USDT balance
    /// e.g., "spot", "futures", "funding", "earn", "bots", "margin"
    /// </summary>
    public Dictionary<string, decimal> AccountBalances { get; set; } = new();

    /// <summary>
    /// Spot account USDT balance (normalized field for convenience)
    /// </summary>
    public decimal SpotUsdt { get; set; }

    /// <summary>
    /// Futures account USDT balance (normalized field for convenience)
    /// </summary>
    public decimal FuturesUsdt { get; set; }

    /// <summary>
    /// Funding account USDT balance (normalized field for convenience)
    /// </summary>
    public decimal FundingUsdt { get; set; }

    /// <summary>
    /// Earn account USDT balance (normalized field for convenience)
    /// </summary>
    public decimal EarnUsdt { get; set; }

    /// <summary>
    /// Bots account USDT balance (normalized field for convenience)
    /// </summary>
    public decimal BotsUsdt { get; set; }

    /// <summary>
    /// Margin account USDT balance (normalized field for convenience)
    /// </summary>
    public decimal MarginUsdt { get; set; }

    /// <summary>
    /// Total USDT balance across all accounts
    /// </summary>
    public decimal TotalUsdt => AccountBalances.Values.Sum();
}
