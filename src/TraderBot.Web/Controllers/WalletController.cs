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
    /// Get account summary with balances across all account types (Bitget V2 API)
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<AccountSummaryDto>> GetAccountSummary(CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _walletService.GetAccountSummaryAsync(cancellationToken);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account summary");
            return StatusCode(500, "Error retrieving account summary");
        }
    }
}
