using Microsoft.AspNetCore.Mvc;
using TraderBot.Application.Ports;
using TraderBot.Contracts.DTOs;

namespace TraderBot.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletService walletService, ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Get wallet balances for all assets
    /// </summary>
    [HttpGet("balances")]
    public async Task<ActionResult<List<WalletBalanceDto>>> GetBalances(CancellationToken cancellationToken)
    {
        try
        {
            var balances = await _walletService.GetAllBalancesAsync(cancellationToken);
            
            var dtos = balances.Select(b => new WalletBalanceDto
            {
                Asset = b.Key,
                Available = b.Value,
                Locked = 0m // TODO: Implement locked balance retrieval from exchange
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet balances");
            return StatusCode(500, "Error retrieving balances");
        }
    }

    /// <summary>
    /// Get balance for a specific asset
    /// </summary>
    [HttpGet("balance/{asset}")]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(string asset, CancellationToken cancellationToken)
    {
        try
        {
            var balance = await _walletService.GetAvailableBalanceAsync(asset, cancellationToken);
            
            return Ok(new WalletBalanceDto
            {
                Asset = asset,
                Available = balance,
                Locked = 0m
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for asset {Asset}", asset);
            return StatusCode(500, $"Error retrieving balance for {asset}");
        }
    }

    /// <summary>
    /// Get account balance summary across all account types (Bitget V2 API)
    /// Uses GET /api/v2/account/all-account-balance endpoint
    /// Requires API key with account read permission
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<AccountBalanceSummaryDto>> GetAccountSummary(CancellationToken cancellationToken)
    {
        try
        {
            var accountBalances = await _walletService.GetAccountSummaryAsync(cancellationToken);
            
            var summary = new AccountBalanceSummaryDto
            {
                AccountBalances = accountBalances
            };

            // Populate normalized fields for convenience
            foreach (var kvp in accountBalances)
            {
                var accountType = kvp.Key.ToLowerInvariant();
                var balance = kvp.Value;

                switch (accountType)
                {
                    case "spot":
                        summary.SpotUsdt = balance;
                        break;
                    case "futures":
                    case "mix_usdt":
                    case "usdt_futures":
                        summary.FuturesUsdt = balance;
                        break;
                    case "funding":
                    case "fund":
                        summary.FundingUsdt = balance;
                        break;
                    case "earn":
                    case "earning":
                        summary.EarnUsdt = balance;
                        break;
                    case "bots":
                    case "copy_trading":
                        summary.BotsUsdt = balance;
                        break;
                    case "margin":
                    case "cross_margin":
                    case "isolated_margin":
                        summary.MarginUsdt = balance;
                        break;
                }
            }

            _logger.LogInformation("Returning account summary with {Count} account types, total: {Total} USDT", 
                accountBalances.Count, summary.TotalUsdt);
            
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account balance summary");
            return StatusCode(500, "Error retrieving account balance summary");
        }
    }
}
