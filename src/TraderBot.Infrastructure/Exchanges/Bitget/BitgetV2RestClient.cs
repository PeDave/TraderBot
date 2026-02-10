using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TraderBot.Domain.Settings;

namespace TraderBot.Infrastructure.Exchanges.Bitget;

/// <summary>
/// Helper class for making Bitget V2 API signed REST requests
/// </summary>
public class BitgetV2RestClient
{
    private readonly HttpClient _httpClient;
    private readonly ExchangeSettings _settings;
    private const string LiveBaseUrl = "https://api.bitget.com";
    private const string TestnetBaseUrl = "https://api.bitget.com"; // Bitget uses same URL for testnet

    public BitgetV2RestClient(HttpClient httpClient, ExchangeSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    /// <summary>
    /// Generate HMAC-SHA256 signature for Bitget V2 API
    /// Prehash = timestamp + method + requestPath + queryString + body
    /// </summary>
    private string GenerateSignature(string timestamp, string method, string requestPath, string queryString = "", string body = "")
    {
        var prehash = timestamp + method.ToUpper() + requestPath + queryString + body;
        var keyBytes = Encoding.UTF8.GetBytes(_settings.ApiSecret);
        var messageBytes = Encoding.UTF8.GetBytes(prehash);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Make a signed GET request to Bitget V2 API
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var baseUrl = _settings.IsTestnet ? TestnetBaseUrl : LiveBaseUrl;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var method = "GET";
        var requestPath = endpoint;

        var signature = GenerateSignature(timestamp, method, requestPath);

        var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + endpoint);
        request.Headers.Add("ACCESS-KEY", _settings.ApiKey);
        request.Headers.Add("ACCESS-SIGN", signature);
        request.Headers.Add("ACCESS-PASSPHRASE", _settings.Passphrase ?? string.Empty);
        request.Headers.Add("ACCESS-TIMESTAMP", timestamp);
        request.Headers.Add("locale", "en-US");
        request.Headers.Add("Content-Type", "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Bitget API request failed: {response.StatusCode} - {content}");
        }

        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
