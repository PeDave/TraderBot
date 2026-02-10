# TraderBot

TraderBot is an extensible, multi-exchange cryptocurrency trading bot platform built with C#/.NET 8. It currently supports Bitget exchange with plans to add BingX and other exchanges in the future.

## 🏗️ Architecture

The solution follows Clean Architecture principles with clear separation of concerns:

```
TraderBot/
├── src/
│   ├── TraderBot.Domain/           # Core domain entities, enums, value objects, and abstractions
│   ├── TraderBot.Contracts/        # DTOs and n8n integration contracts
│   ├── TraderBot.Application/      # Business logic, strategies, orchestration
│   ├── TraderBot.Infrastructure/   # External services, EF Core, exchange adapters
│   ├── TraderBot.Web/              # ASP.NET Core API and dashboard
│   └── TraderBot.Worker/           # Background service for bot execution
```

### Project Descriptions

- **TraderBot.Domain**: Contains core business entities (Candle, Order, Position), enums (ExchangeType, OrderSide, TimeFrame), value objects (Symbol, Price), and abstractions (IExchangeClient, IMarketDataFeed, ITradeExecutor)

- **TraderBot.Contracts**: Data transfer objects for API communication and n8n webhook integration

- **TraderBot.Application**: Business logic including:
  - Bot orchestrator for coordinating market data and strategy execution
  - Martingale strategy skeleton (⚠️ high-risk, see warnings below)
  - Dow Theory technical analyzer placeholder
  - Risk management rules
  - Application ports (IMarketDataStore, IWalletService, IPositionRepository)

- **TraderBot.Infrastructure**: Infrastructure implementations:
  - EF Core with SQLite for data persistence
  - Bitget exchange adapter (skeleton with TODOs for Bitget.Net integration)
  - Wallet and trade execution services
  - Multi-exchange router placeholder

- **TraderBot.Web**: ASP.NET Core web application with:
  - RESTful API endpoints
  - Swagger/OpenAPI documentation
  - Identity authentication (seeded admin user)
  - Controllers for wallet, market data, bot control, and n8n analysis

- **TraderBot.Worker**: .NET Worker Service that:
  - Runs the bot orchestrator in the background
  - Manages market data feed subscription
  - Executes trading strategies
  - Performs database migrations on startup

## ⚙️ Configuration

### Exchange Settings

Configure your exchange credentials in `appsettings.json`:

```json
{
  "Exchange": {
    "ExchangeType": "Bitget",
    "ApiKey": "YOUR_API_KEY_HERE",
    "ApiSecret": "YOUR_API_SECRET_HERE",
    "Passphrase": "YOUR_PASSPHRASE_HERE",
    "IsTestnet": true,
    "AccountType": "spot"
  }
}
```

**Important Notes:**
- **ApiKey/ApiSecret/Passphrase**: Obtain these from your Bitget account settings > API Management
  - For Bitget, the Passphrase is **required** (created when you generate the API key)
  - Make sure IP whitelist is either disabled or includes your server IP
  - Enable appropriate permissions: Read for market-data-only, Read+Trade for trading
- **IsTestnet**: Set to `true` for testnet/sandbox, `false` for production trading
- **AccountType**: Use `"spot"` for spot trading, `"futures"` for futures (futures support is limited in current version)

⚠️ **Security**: Never commit real API keys to source control. Use environment variables or Azure Key Vault for production deployments.

### Trading Settings

Control trading execution and error handling:

```json
{
  "Trading": {
    "Enabled": false,
    "RequireBalanceCheck": false,
    "AccountType": "spot",
    "MaxRetries": 3,
    "RetryDelaySeconds": 2,
    "UseExponentialBackoff": true
  }
}
```

**Settings Explained:**
- **Enabled**: `false` = market-data-only mode (no trading, no balance checks), `true` = enable trading
- **RequireBalanceCheck**: When `true`, the bot stops if balance checks fail; when `false`, continues with 0 balance fallback
- **AccountType**: Must match the account type you want to query (`"spot"` or `"futures"`)
- **MaxRetries**: Number of retry attempts for failed API calls (default: 3)
- **RetryDelaySeconds**: Initial delay between retries in seconds (default: 2)
- **UseExponentialBackoff**: When `true`, doubles the delay after each retry (2s → 4s → 8s)

**Recommended Configurations:**

*Market Data Collection Only:*
```json
{
  "Trading": {
    "Enabled": false,
    "RequireBalanceCheck": false
  }
}
```

*Production Trading:*
```json
{
  "Trading": {
    "Enabled": true,
    "RequireBalanceCheck": true,
    "AccountType": "spot",
    "MaxRetries": 3
  }
}
```

### Bot Settings

Configure trading parameters:

```json
{
  "Bot": {
    "Symbol": "BTCUSDT",
    "TimeFrame": "FiveMinutes",
    "InitialCapital": 100.0,
    "MaxDrawdown": 0.20,
    "MaxMartingaleSteps": 5,
    "MartingaleMultiplier": 2.0
  }
}
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or later (recommended) or VS Code
- SQLite (included with .NET)

### Building the Solution

```bash
# Clone the repository
git clone https://github.com/PeDave/TraderBot.git
cd TraderBot

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests (if available)
dotnet test
```

### Running the Web API

```bash
cd src/TraderBot.Web
dotnet run
```

The API will be available at `https://localhost:5001` (or the port shown in the console).

Access Swagger UI at: `https://localhost:5001/swagger`

**Default Admin Credentials:**
- Email: `admin@traderbot.local`
- Password: `Admin123!`

### Running the Worker Service

```bash
cd src/TraderBot.Worker
dotnet run
```

The worker will:
1. Run database migrations
2. Start the bot orchestrator
3. Subscribe to market data feed
4. Execute trading strategies based on incoming candles

## 📡 API Endpoints

### Wallet

- `GET /api/wallet/balances` - Get all wallet balances
- `GET /api/wallet/balance/{asset}` - Get balance for specific asset

### Market Data

- `GET /api/marketdata/candles?symbol=BTCUSDT&from=...&to=...` - Get historical candles
- `GET /api/marketdata/candles/latest?symbol=BTCUSDT` - Get latest candle

### Bot Control

- `GET /api/bot/status` - Get current bot status
- `POST /api/bot/start` - Start the trading bot
- `POST /api/bot/stop` - Stop the trading bot

### Analysis (n8n Integration)

- `POST /api/analysis/result` - Receive analysis results from n8n

## ⚠️ Martingale Strategy Warning

**IMPORTANT**: The included Martingale strategy is a SKELETON implementation for demonstration purposes only.

**Martingale is an extremely high-risk strategy** that doubles position size after each loss. This can lead to:
- Rapid capital depletion during losing streaks
- Margin calls and liquidation
- Exponential losses

**Before using this strategy in live trading, you MUST implement:**

1. ✅ Comprehensive position tracking and management
2. ✅ Stop-loss mechanisms
3. ✅ Maximum drawdown protection
4. ✅ Real-time account balance monitoring
5. ✅ Emergency shutdown procedures
6. ✅ Extensive backtesting with historical data
7. ✅ Risk analysis and stress testing

**USE AT YOUR OWN RISK. We accept no responsibility for trading losses.**

## 🔧 Development Status

### ✅ Completed

- Multi-project solution structure
- Domain model and abstractions
- EF Core persistence with SQLite
- ASP.NET Core API with Swagger
- Identity authentication
- Worker service skeleton
- Configuration management

### 🚧 TODO

The following items need implementation when integrating with actual exchanges:

**Bitget Integration** (marked with TODO comments):
- Install and configure `Bitget.Net` NuGet package (when available/stable)
- Implement REST API calls in `BitgetExchangeClient`:
  - Connection testing
  - Balance queries
  - Order placement
  - Order cancellation
- Implement WebSocket subscriptions in `BitgetMarketDataFeed`:
  - Kline/candlestick data streaming
  - Real-time event handling

**Strategy Enhancements**:
- Complete Dow Theory analyzer implementation
- Add more technical indicators
- Implement additional trading strategies
- Enhance risk management rules

**Multi-Exchange Support**:
- Add BingX adapter
- Implement exchange router for strategy selection
- Unified exchange abstraction layer

## 🗄️ Database

The solution uses SQLite for both trading data and identity management:

- `trading.db` - Stores candles, orders, and positions
- `identity.db` - Stores user accounts and roles

Migrations are automatically applied on startup for both projects.

### Manual Migrations

```bash
# Add a new migration (from Infrastructure project directory)
cd src/TraderBot.Infrastructure
dotnet ef migrations add MigrationName --context TradingDbContext --startup-project ../TraderBot.Web

# Update database
dotnet ef database update --context TradingDbContext --startup-project ../TraderBot.Web
```

## 🤝 n8n Integration

TraderBot can integrate with n8n for advanced market analysis:

1. n8n workflow analyzes market data
2. Workflow sends results to `/api/analysis/result` endpoint
3. TraderBot processes signals and executes trades

See `TraderBot.Contracts.N8n` for request/response schemas.

## 📝 License

[Specify your license here]

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 🔍 Troubleshooting

### Balance Check Failures

**Symptoms:**
- Logs show errors like:
  - Old version: "Failed to get balance: 92.249.239.147" or "Failed to get balance: (null)"
  - New version: "Failed to get spot balance - Error Code: {Code}, Message: {Message}"
- Worker exits with code -1 (in older versions without this fix)
- Balance requests time out or return errors

**Solutions:**

1. **Check API Credentials:**
   - Verify ApiKey, ApiSecret, and Passphrase are correct
   - Ensure Passphrase is not null or empty (required for Bitget)
   - Test credentials on Bitget website first

2. **IP Whitelist:**
   - Disable IP whitelist in Bitget API settings, OR
   - Add your server's IP address to the whitelist
   - Note: The error message often shows an IP address that failed authentication

3. **API Permissions:**
   - Ensure API key has "Read" permission for balance checks
   - Add "Trade" permission if trading is enabled
   - Regenerate API key if permissions were changed

4. **Use Market-Data-Only Mode:**
   ```json
   {
     "Trading": {
       "Enabled": false,
       "RequireBalanceCheck": false
     }
   }
   ```
   This allows the Worker to run and collect market data even if balance checks fail.

5. **Check Account Type:**
   - Ensure `Trading:AccountType` matches your intended account type
   - Use `"spot"` for spot trading (most common)
   - Futures balance queries have limited support in current version

### Worker Keeps Crashing

**Solution:** The worker has been updated with graceful error handling. Update to the latest version and use the `Trading:Enabled=false` configuration for stable market-data collection.

### WebSocket Connection Issues

- Verify internet connectivity
- Check if Bitget API is operational (status.bitget.com)
- Ensure firewall allows outbound WebSocket connections
- Try switching `IsTestnet` setting

### Logs Show "Futures account balance query is not fully implemented"

This is expected if `AccountType` is set to `"futures"`. The current Bitget.Net library version may not have the required API endpoints for futures balance queries. Options:
- Use `"spot"` account type if spot trading meets your needs, OR
- Set `Trading:Enabled=false` for market-data-only mode, OR
- Wait for library updates that support futures balance queries

## ⚡ Support

For issues and questions, please open an issue on GitHub.

---

**Disclaimer**: This software is for educational purposes. Cryptocurrency trading carries significant risk. Always do your own research and never invest more than you can afford to lose.