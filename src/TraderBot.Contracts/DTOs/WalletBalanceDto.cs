namespace TraderBot.Contracts.DTOs;

/// <summary>
/// Wallet balance data transfer object
/// </summary>
public class WalletBalanceDto
{
    public string Asset { get; set; } = string.Empty;
    public decimal Available { get; set; }
    public decimal Locked { get; set; }
    public decimal Total => Available + Locked;
}
