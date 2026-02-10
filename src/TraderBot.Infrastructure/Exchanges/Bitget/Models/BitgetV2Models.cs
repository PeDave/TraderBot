using System.Text.Json.Serialization;

namespace TraderBot.Infrastructure.Exchanges.Bitget.Models;

/// <summary>
/// Bitget V2 API response wrapper
/// </summary>
internal class BitgetV2Response<T>
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("requestTime")]
    public long RequestTime { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    public bool IsSuccess => Code == "00000";
}

/// <summary>
/// Account balance item from Bitget V2 all-account-balance endpoint
/// </summary>
internal class BitgetAccountBalance
{
    [JsonPropertyName("accountType")]
    public string AccountType { get; set; } = string.Empty;

    [JsonPropertyName("usdtBalance")]
    public string UsdtBalance { get; set; } = "0";
}

/// <summary>
/// List wrapper for account balances
/// </summary>
internal class BitgetAccountBalanceList
{
    [JsonPropertyName("totalUSDT")]
    public string TotalUSDT { get; set; } = "0";

    [JsonPropertyName("accountList")]
    public List<BitgetAccountBalance> AccountList { get; set; } = new();
}
