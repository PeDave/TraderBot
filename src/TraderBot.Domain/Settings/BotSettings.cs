using TraderBot.Domain.Enums;

namespace TraderBot.Domain.Settings;

/// <summary>
/// Trading bot configuration
/// </summary>
public class BotSettings
{
    public string Symbol { get; set; } = "BTCUSDT";
    public TimeFrame TimeFrame { get; set; } = TimeFrame.FiveMinutes;
    public decimal InitialCapital { get; set; } = 100m;
    public decimal MaxDrawdown { get; set; } = 0.20m; // 20% max drawdown
    public int MaxMartingaleSteps { get; set; } = 5; // Maximum number of martingale steps
    public decimal MartingaleMultiplier { get; set; } = 2.0m; // Position size multiplier for martingale
}
