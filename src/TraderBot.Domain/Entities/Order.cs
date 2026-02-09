using TraderBot.Domain.Enums;

namespace TraderBot.Domain.Entities;

/// <summary>
/// Represents a trade order
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public OrderType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? FilledPrice { get; set; }
    public string Status { get; set; } = string.Empty; // New, Filled, PartiallyFilled, Cancelled, Rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FilledAt { get; set; }
}
