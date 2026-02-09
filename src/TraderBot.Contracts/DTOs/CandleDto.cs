namespace TraderBot.Contracts.DTOs;

/// <summary>
/// Candle data transfer object
/// </summary>
public class CandleDto
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
}
