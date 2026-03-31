using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TraderBot.Application.Analyzers;
using TraderBot.Application.Orchestration;
using TraderBot.Application.Ports;
using TraderBot.Application.RiskManagement;
using TraderBot.Application.Strategies;
using TraderBot.Domain.Abstractions;
using TraderBot.Domain.Enums;
using TraderBot.Domain.Settings;
using TraderBot.Infrastructure.Exchanges.Bitget;
using TraderBot.Infrastructure.Persistence;
using TraderBot.Infrastructure.Services;
using TraderBot.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TraderBot API", Version = "v1" });
});

// Configure databases
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TradingDb") ?? "Data Source=trading.db"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("IdentityDb") ?? "Data Source=identity.db"));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure settings
var exchangeSettings = builder.Configuration.GetSection("Exchange").Get<ExchangeSettings>() ?? new ExchangeSettings();
var botSettings = builder.Configuration.GetSection("Bot").Get<BotSettings>() ?? new BotSettings();
var tradingSettings = builder.Configuration.GetSection("Trading").Get<TradingSettings>() ?? new TradingSettings();

builder.Services.AddSingleton(exchangeSettings);
builder.Services.AddSingleton(botSettings);
builder.Services.AddSingleton(tradingSettings);

// Register HttpClient for API calls
builder.Services.AddHttpClient();

// Register application services
builder.Services.AddScoped<IMarketDataStore, EfMarketDataStore>();
builder.Services.AddScoped<IPositionRepository, EfPositionRepository>();

// Register exchange services (currently Bitget)
builder.Services.AddSingleton<IExchangeClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<BitgetExchangeClient>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    return new BitgetExchangeClient(logger, exchangeSettings, httpClient);
});
builder.Services.AddSingleton<IMarketDataFeed, BitgetMarketDataFeed>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITradeExecutor, TradeExecutor>();

// Register strategy and analysis
builder.Services.AddSingleton<DowTheoryAnalyzer>();
builder.Services.AddSingleton<RiskManager>();
builder.Services.AddScoped<MartingaleStrategy>();

// Register bot orchestrator as singleton to maintain state
builder.Services.AddSingleton<BotOrchestrator>();

var app = builder.Build();

// Seed database and create admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Migrate trading database
        var tradingDb = services.GetRequiredService<TradingDbContext>();
        tradingDb.Database.Migrate();

        // Migrate identity database
        var identityDb = services.GetRequiredService<ApplicationDbContext>();
        identityDb.Database.Migrate();

        // Seed admin user
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = "admin@traderbot.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                app.Logger.LogInformation("Admin user created: {Email}", adminEmail);
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred seeding the database");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
