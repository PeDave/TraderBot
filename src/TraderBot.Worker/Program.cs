using Microsoft.EntityFrameworkCore;
using TraderBot.Application.Analyzers;
using TraderBot.Application.Orchestration;
using TraderBot.Application.Ports;
using TraderBot.Application.RiskManagement;
using TraderBot.Application.Strategies;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Settings;
using TraderBot.Infrastructure.Exchanges.Bitget;
using TraderBot.Infrastructure.Persistence;
using TraderBot.Infrastructure.Services;
using TraderBot.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add HttpClient for Bitget V2 API calls
builder.Services.AddHttpClient();

// Configure databases
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TradingDb") ?? "Data Source=trading.db"));

// Configure settings
var exchangeSettings = builder.Configuration.GetSection("Exchange").Get<ExchangeSettings>() ?? new ExchangeSettings();
var botSettings = builder.Configuration.GetSection("Bot").Get<BotSettings>() ?? new BotSettings();
var tradingSettings = builder.Configuration.GetSection("Trading").Get<TradingSettings>() ?? new TradingSettings();

builder.Services.AddSingleton(exchangeSettings);
builder.Services.AddSingleton(botSettings);
builder.Services.AddSingleton(tradingSettings);

// Register application services
builder.Services.AddScoped<IMarketDataStore, EfMarketDataStore>();
builder.Services.AddScoped<IPositionRepository, EfPositionRepository>();

// Register exchange services (currently Bitget)
builder.Services.AddSingleton<IExchangeClient, BitgetExchangeClient>();
builder.Services.AddSingleton<IMarketDataFeed, BitgetMarketDataFeed>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITradeExecutor, TradeExecutor>();

// Register strategy and analysis
builder.Services.AddSingleton<DowTheoryAnalyzer>();
builder.Services.AddSingleton<RiskManager>();
builder.Services.AddScoped<MartingaleStrategy>();

// Register bot orchestrator
builder.Services.AddSingleton<BotOrchestrator>();

// Register the worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Run database migrations on startup
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TradingDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
    }
}

host.Run();
