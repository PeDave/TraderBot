namespace TraderBot.Domain.ValueObjects;

/// <summary>
/// Represents a price with its currency
/// </summary>
public record Price
{
    public decimal Value { get; init; }
    public string Currency { get; init; }

    public Price(decimal value, string currency)
    {
        if (value < 0)
            throw new ArgumentException("Price cannot be negative", nameof(value));
        
        Value = value;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public override string ToString() => $"{Value} {Currency}";
}
