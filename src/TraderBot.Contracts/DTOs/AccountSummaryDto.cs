namespace TraderBot.Contracts.DTOs;

/// <summary>
/// Aggregated account summary with balances per account type
/// </summary>
public class AccountSummaryDto
{
    /// <summary>
    /// Raw list of account balances
    /// </summary>
    public List<AccountBalanceDto> Balances { get; set; } = new();

    /// <summary>
    /// Convenience dictionary mapping account type to USDT balance
    /// </summary>
    public Dictionary<string, decimal> BalancesByType { get; set; } = new();

    /// <summary>
    /// Total USDT across all account types
    /// </summary>
    public decimal TotalUsdt { get; set; }

    // Convenience properties for common account types
    public decimal SpotBalance { get; set; }
    public decimal FuturesBalance { get; set; }
    public decimal FundingBalance { get; set; }
    public decimal EarnBalance { get; set; }
    public decimal BotsBalance { get; set; }
    public decimal MarginBalance { get; set; }
}
