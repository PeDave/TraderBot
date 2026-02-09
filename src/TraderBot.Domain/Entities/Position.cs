using TraderBot.Domain.Enums;

namespace TraderBot.Domain.Entities;

/// <summary>
/// Represents a trading position
/// </summary>
public class Position
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public OrderSide Side { get; set; }
    public int MartingaleStep { get; set; } = 0; // Track martingale progression
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public bool IsOpen { get; set; } = true;
}
