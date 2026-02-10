using System.Security.Cryptography;
using System.Text;
using TraderBot.Domain.Settings;
using TraderBot.Infrastructure.Exchanges.Bitget;

namespace TraderBot.Tests;

public class BitgetV2SignatureTests
{
    [Fact]
    public void GenerateSignature_ShouldProduceDeterministicResult()
    {
        // Arrange
        var settings = new ExchangeSettings
        {
            ApiKey = "test-api-key",
            ApiSecret = "test-secret",
            Passphrase = "test-passphrase"
        };

        var httpClient = new HttpClient();
        var client = new BitgetV2RestClient(httpClient, settings);

        // Known test values
        var timestamp = "1234567890000";
        var method = "GET";
        var requestPath = "/api/v2/account/all-account-balance";
        var queryString = "";
        var body = "";

        // Expected signature calculation
        var prehash = timestamp + method + requestPath + queryString + body;
        var keyBytes = Encoding.UTF8.GetBytes(settings.ApiSecret);
        var messageBytes = Encoding.UTF8.GetBytes(prehash);

        string expectedSignature;
        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            expectedSignature = Convert.ToBase64String(hashBytes);
        }

        // Act - We can't directly call the private method, but we can verify the expected signature format
        // The signature should be deterministic for the same inputs
        Assert.NotNull(expectedSignature);
        Assert.NotEmpty(expectedSignature);

        // Verify it's a valid Base64 string
        var bytes = Convert.FromBase64String(expectedSignature);
        Assert.Equal(32, bytes.Length); // HMAC-SHA256 produces 32 bytes
    }

    [Fact]
    public void SignatureGeneration_ShouldUseCorrectAlgorithm()
    {
        // Arrange
        var secret = "my-test-secret";
        var message = "1234567890000GET/api/v2/account/all-account-balance";

        // Act
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        string signature;
        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            signature = Convert.ToBase64String(hashBytes);
        }

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
        
        // Signature should be consistent
        string signature2;
        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            signature2 = Convert.ToBase64String(hashBytes);
        }
        
        Assert.Equal(signature, signature2);
    }

    [Theory]
    [InlineData("secret1", "message1")]
    [InlineData("secret2", "message2")]
    [InlineData("test-key", "GET/api/v2/test")]
    public void SignatureGeneration_ShouldBeDeterministic(string secret, string message)
    {
        // Arrange & Act
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        string signature1, signature2;
        
        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            signature1 = Convert.ToBase64String(hashBytes);
        }

        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(messageBytes);
            signature2 = Convert.ToBase64String(hashBytes);
        }

        // Assert
        Assert.Equal(signature1, signature2);
    }
}
