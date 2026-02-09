using TraderBot.Domain.Enums;

namespace TraderBot.Domain.Abstractions;

/// <summary>
/// Interface for receiving real-time market data
/// </summary>
public interface IMarketDataFeed
{
    event EventHandler<CandleReceivedEventArgs>? OnCandleReceived;
    
    Task StartAsync(string symbol, TimeFrame timeFrame, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

public class CandleReceivedEventArgs : EventArgs
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public string Symbol { get; set; } = string.Empty;
}
