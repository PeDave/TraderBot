namespace TraderBot.Contracts.DTOs;

/// <summary>
/// Represents a single account balance entry from Bitget V2 API
/// </summary>
public class AccountBalanceDto
{
    /// <summary>
    /// Account type (e.g., spot, futures, funding, earn, bots, margin)
    /// </summary>
    public string AccountType { get; set; } = string.Empty;

    /// <summary>
    /// USDT balance value for this account type
    /// </summary>
    public decimal UsdtBalance { get; set; }
}
