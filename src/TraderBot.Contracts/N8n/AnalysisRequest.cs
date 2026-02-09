namespace TraderBot.Contracts.N8n;

/// <summary>
/// Request sent to n8n for market analysis
/// </summary>
public class AnalysisRequest
{
    public string Symbol { get; set; } = string.Empty;
    public string TimeFrame { get; set; } = string.Empty;
    public List<CandleData> Candles { get; set; } = new();
    public Dictionary<string, object> Indicators { get; set; } = new();
}

public class CandleData
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
