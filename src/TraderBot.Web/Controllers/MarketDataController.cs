using Microsoft.AspNetCore.Mvc;
using TraderBot.Application.Ports;
using TraderBot.Contracts.DTOs;

namespace TraderBot.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataStore _marketDataStore;
    private readonly ILogger<MarketDataController> _logger;

    public MarketDataController(IMarketDataStore marketDataStore, ILogger<MarketDataController> logger)
    {
        _marketDataStore = marketDataStore;
        _logger = logger;
    }

    /// <summary>
    /// Get historical candles for a symbol within a time range
    /// </summary>
    [HttpGet("candles")]
    public async Task<ActionResult<List<CandleDto>>> GetCandles(
        [FromQuery] string symbol,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        try
        {
            var candles = await _marketDataStore.GetCandlesAsync(symbol, from, to, cancellationToken);
            
            var dtos = candles.Select(c => new CandleDto
            {
                Symbol = c.Symbol,
                Timestamp = c.Timestamp,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume,
                TimeFrame = c.TimeFrame
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting candles for {Symbol}", symbol);
            return StatusCode(500, "Error retrieving market data");
        }
    }

    /// <summary>
    /// Get the latest candle for a symbol
    /// </summary>
    [HttpGet("candles/latest")]
    public async Task<ActionResult<CandleDto>> GetLatestCandle(
        [FromQuery] string symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            var candle = await _marketDataStore.GetLatestCandleAsync(symbol, cancellationToken);
            
            if (candle == null)
            {
                return NotFound($"No candles found for symbol {symbol}");
            }

            var dto = new CandleDto
            {
                Symbol = candle.Symbol,
                Timestamp = candle.Timestamp,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume,
                TimeFrame = candle.TimeFrame
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest candle for {Symbol}", symbol);
            return StatusCode(500, "Error retrieving latest market data");
        }
    }
}
