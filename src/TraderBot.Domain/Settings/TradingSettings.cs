namespace TraderBot.Domain.Settings;

/// <summary>
/// Trading execution configuration
/// </summary>
public class TradingSettings
{
    /// <summary>
    /// Enable/disable trading execution (false = market-data-only mode)
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Require successful balance check before trading (when Enabled = true)
    /// Default is false to allow graceful operation even with API credential issues
    /// </summary>
    public bool RequireBalanceCheck { get; set; } = false;

    /// <summary>
    /// Account type to use for balance checks: "spot" or "futures"
    /// </summary>
    public string AccountType { get; set; } = "spot";

    /// <summary>
    /// Maximum number of retries for failed API calls
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay in seconds for retry backoff
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}
