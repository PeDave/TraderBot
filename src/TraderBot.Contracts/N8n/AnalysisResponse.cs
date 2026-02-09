namespace TraderBot.Contracts.N8n;

/// <summary>
/// Response received from n8n with market analysis results
/// </summary>
public class AnalysisResponse
{
    public string Symbol { get; set; } = string.Empty;
    public string Signal { get; set; } = string.Empty; // "BUY", "SELL", "HOLD"
    public decimal Confidence { get; set; } // 0.0 - 1.0
    public string? Reason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
