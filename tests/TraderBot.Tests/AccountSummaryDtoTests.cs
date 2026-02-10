using System.Text.Json;
using TraderBot.Contracts.DTOs;

namespace TraderBot.Tests;

public class AccountSummaryDtoTests
{
    [Fact]
    public void AccountBalanceDto_ShouldSerializeCorrectly()
    {
        // Arrange
        var dto = new AccountBalanceDto
        {
            AccountType = "spot",
            UsdtBalance = 1000.50m
        };

        // Act
        var json = JsonSerializer.Serialize(dto);
        var deserialized = JsonSerializer.Deserialize<AccountBalanceDto>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("spot", deserialized.AccountType);
        Assert.Equal(1000.50m, deserialized.UsdtBalance);
    }

    [Fact]
    public void AccountSummaryDto_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var summary = new AccountSummaryDto
        {
            TotalUsdt = 5000.00m,
            Balances = new List<AccountBalanceDto>
            {
                new() { AccountType = "spot", UsdtBalance = 1000.00m },
                new() { AccountType = "futures", UsdtBalance = 2000.00m },
                new() { AccountType = "funding", UsdtBalance = 1500.00m },
                new() { AccountType = "earn", UsdtBalance = 500.00m }
            }
        };

        // Act & Assert
        Assert.Equal(5000.00m, summary.TotalUsdt);
        Assert.Equal(4, summary.Balances.Count);
    }

    [Fact]
    public void AccountSummaryDto_ShouldPopulateBalancesByType()
    {
        // Arrange
        var summary = new AccountSummaryDto
        {
            BalancesByType = new Dictionary<string, decimal>
            {
                { "spot", 1000.00m },
                { "futures", 2000.00m },
                { "funding", 1500.00m }
            }
        };

        // Act & Assert
        Assert.Equal(3, summary.BalancesByType.Count);
        Assert.Equal(1000.00m, summary.BalancesByType["spot"]);
        Assert.Equal(2000.00m, summary.BalancesByType["futures"]);
        Assert.Equal(1500.00m, summary.BalancesByType["funding"]);
    }

    [Fact]
    public void AccountSummaryDto_ShouldHaveConvenienceProperties()
    {
        // Arrange
        var summary = new AccountSummaryDto
        {
            SpotBalance = 1000.00m,
            FuturesBalance = 2000.00m,
            FundingBalance = 1500.00m,
            EarnBalance = 500.00m,
            BotsBalance = 200.00m,
            MarginBalance = 300.00m
        };

        // Act & Assert
        Assert.Equal(1000.00m, summary.SpotBalance);
        Assert.Equal(2000.00m, summary.FuturesBalance);
        Assert.Equal(1500.00m, summary.FundingBalance);
        Assert.Equal(500.00m, summary.EarnBalance);
        Assert.Equal(200.00m, summary.BotsBalance);
        Assert.Equal(300.00m, summary.MarginBalance);
    }

    [Fact]
    public void AccountSummaryDto_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var summary = new AccountSummaryDto();

        // Assert
        Assert.NotNull(summary.Balances);
        Assert.NotNull(summary.BalancesByType);
        Assert.Empty(summary.Balances);
        Assert.Empty(summary.BalancesByType);
        Assert.Equal(0m, summary.TotalUsdt);
        Assert.Equal(0m, summary.SpotBalance);
        Assert.Equal(0m, summary.FuturesBalance);
    }

    [Fact]
    public void AccountSummaryDto_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var original = new AccountSummaryDto
        {
            TotalUsdt = 5000.00m,
            SpotBalance = 1000.00m,
            FuturesBalance = 2000.00m,
            Balances = new List<AccountBalanceDto>
            {
                new() { AccountType = "spot", UsdtBalance = 1000.00m },
                new() { AccountType = "futures", UsdtBalance = 2000.00m }
            },
            BalancesByType = new Dictionary<string, decimal>
            {
                { "spot", 1000.00m },
                { "futures", 2000.00m }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AccountSummaryDto>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(5000.00m, deserialized.TotalUsdt);
        Assert.Equal(1000.00m, deserialized.SpotBalance);
        Assert.Equal(2000.00m, deserialized.FuturesBalance);
        Assert.Equal(2, deserialized.Balances.Count);
        Assert.Equal(2, deserialized.BalancesByType.Count);
    }
}
