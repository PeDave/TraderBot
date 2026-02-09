using TraderBot.Domain.Enums;

namespace TraderBot.Domain.ValueObjects;

/// <summary>
/// Represents a trading symbol (e.g., BTCUSDT)
/// </summary>
public record Symbol
{
    public string Base { get; init; }
    public string Quote { get; init; }

    public Symbol(string @base, string quote)
    {
        Base = @base ?? throw new ArgumentNullException(nameof(@base));
        Quote = quote ?? throw new ArgumentNullException(nameof(quote));
    }

    public override string ToString() => $"{Base}{Quote}";

    public static Symbol Parse(string symbol)
    {
        // Simple parser - in production, would need more sophisticated parsing
        if (symbol.EndsWith("USDT"))
        {
            return new Symbol(symbol[..^4], "USDT");
        }
        if (symbol.EndsWith("USD"))
        {
            return new Symbol(symbol[..^3], "USD");
        }
        throw new ArgumentException($"Unable to parse symbol: {symbol}");
    }
}
