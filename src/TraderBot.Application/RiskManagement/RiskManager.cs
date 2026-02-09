using TraderBot.Domain.Settings;

namespace TraderBot.Application.RiskManagement;

/// <summary>
/// Risk management rules for the trading bot
/// </summary>
public class RiskManager
{
    private readonly BotSettings _settings;

    public RiskManager(BotSettings settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Checks if a new position can be opened based on current account state
    /// </summary>
    public bool CanOpenPosition(decimal accountBalance, decimal currentDrawdown)
    {
        if (currentDrawdown > _settings.MaxDrawdown)
        {
            return false;
        }

        if (accountBalance <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates position size based on account balance and risk parameters
    /// </summary>
    public decimal CalculatePositionSize(decimal accountBalance, int martingaleStep)
    {
        if (martingaleStep >= _settings.MaxMartingaleSteps)
        {
            return 0;
        }

        // Base position size (e.g., 1% of account)
        var baseSize = accountBalance * 0.01m;
        
        // Apply martingale multiplier
        var multiplier = (decimal)Math.Pow((double)_settings.MartingaleMultiplier, martingaleStep);
        
        return baseSize * multiplier;
    }

    /// <summary>
    /// Determines if we should apply martingale (increase position after loss)
    /// </summary>
    public bool ShouldApplyMartingale(int currentStep, decimal loss)
    {
        return currentStep < _settings.MaxMartingaleSteps && loss < 0;
    }
}
