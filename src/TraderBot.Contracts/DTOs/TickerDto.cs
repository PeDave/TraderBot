namespace TraderBot.Contracts.DTOs;

/// <summary>
/// Ticker/price data transfer object
/// </summary>
public class TickerDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal High24h { get; set; }
    public decimal Low24h { get; set; }
    public decimal Volume24h { get; set; }
    public DateTime Timestamp { get; set; }
}
