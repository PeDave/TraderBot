namespace TraderBot.Domain.Entities;

/// <summary>
/// Represents a candlestick/OHLCV data point stored in the database
/// </summary>
public class Candle
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
