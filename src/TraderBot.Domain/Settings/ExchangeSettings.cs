using TraderBot.Domain.Enums;

namespace TraderBot.Domain.Settings;

/// <summary>
/// Exchange-specific configuration
/// </summary>
public class ExchangeSettings
{
    public ExchangeType ExchangeType { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string? Passphrase { get; set; } // For exchanges that require it
    public bool IsTestnet { get; set; } = true;
}
