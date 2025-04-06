using System.Text.Json;
using Common;
using Common.Data.Enums;
using Common.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Common.Data.Dtos;

namespace ApiServer.Services;

/// <summary>
/// A SignalR service for managing real-time communication between the server and Blazor clients.
/// Handles wallet updates, order book changes, and BTC rate updates.
/// </summary>
public class BlazorSignalRService
{
    private readonly IServiceScopeFactory scopeFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorSignalRService"/> class.
    /// </summary>
    public BlazorSignalRService(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    private Dictionary<string, string> _subscriptions = new Dictionary<string, string>();

    /// <summary>
    /// Subscribes a client to wallet updates by linking the user's account ID to their SignalR connection ID.
    /// </summary>
    /// <param name="accountGuid">The user's account GUID.</param>
    /// <param name="connectionId">The SignalR connection ID.</param>
    public async Task SubscribeToWalletUpdates(string accountGuid, string connectionId)
    {
        _subscriptions[accountGuid] = connectionId;
    }

    /// <summary>
    /// Sends a wallet update to a specific client based on their account ID.
    /// </summary>
    /// <param name="clients">The SignalR clients proxy.</param>
    /// <param name="accountGuid">The user's account GUID.</param>
    /// <param name="wallet">The wallet data to send.</param>
    public async Task SendWalletUpdate(IHubClients clients, string accountGuid, AccountWalletDto wallet)
    {
        if (_subscriptions.TryGetValue(accountGuid, out var connectionId))
        {
            await clients.Client(connectionId).SendAsync("WalletUpdate", wallet);
        }
    }

    /// <summary>
    /// Broadcasts aggregated order book updates (bids and asks) to all connected clients.
    /// </summary>
    /// <param name="clients">The SignalR clients proxy.</param>
    /// <param name="bidsAgg">List of aggregated bid orders.</param>
    /// <param name="asksAgg">List of aggregated ask orders.</param>
    public async Task SendOrdersUpdate(IHubClients clients, OrderBookSnapshotDto snapshot)
    {
        await clients.All.SendAsync("OrdersUpdate", snapshot);
    }

    /// <summary>
    /// Broadcasts a new Bitcoin rate to all connected clients.
    /// </summary>
    /// <param name="clients">The SignalR clients proxy.</param>
    /// <param name="newRate">The new BTC exchange rate to send.</param>
    public async Task SendBitcoinRateUpdate(IHubClients clients, decimal newRate)
    {
        await clients.All.SendAsync("BtcRateUpdate", newRate);
    }

    /// <summary>
    /// Fetches the user's wallet balances (crypto and fiat) and sends them to the specified client.
    /// </summary>
    /// <param name="clients">The SignalR caller clients proxy.</param>
    /// <param name="connectionId">The SignalR connection ID of the requesting client.</param>
    /// <param name="accountGuid">The user's account GUID.</param>
    public async Task ClientGetUserBalance(IHubCallerClients clients, string connectionId, string accountGuid)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<SqlContext>();
        var accountId = await dbContext.Accounts.Where(x => x.AccountId == accountGuid).Select(x => x.Id).FirstAsync();

        var cryptoWallet = await WalletService.GetOrCreateWallet(dbContext, accountId, Constants.CryptoCurrency);
        var fiatWallet = await WalletService.GetOrCreateWallet(dbContext, accountId, Constants.FiatCurrency);

        await clients.Client(connectionId).SendAsync("WalletUpdate", new AccountWalletDto(cryptoWallet));
        await clients.Client(connectionId).SendAsync("WalletUpdate", new AccountWalletDto(fiatWallet));
    }

    /// <summary>
    /// Sends the latest aggregated open bid and ask orders to a specific client.
    /// </summary>
    /// <param name="clients">The SignalR caller clients proxy.</param>
    /// <param name="connectionId">The SignalR connection ID of the requesting client.</param>
    public async Task ClientGetOrders(IHubCallerClients clients, string connectionId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<SqlContext>();
        var snapshot = await dbContext.OrderBookSnapshots.OrderByDescending(x => x.Id).AsNoTracking().FirstOrDefaultAsync();
        if (snapshot is null)
            return;
        var bidsAgg = JsonSerializer.Deserialize<List<BitcoinOrdersDto>>(snapshot.BidsJson)!;
        var asksAgg = JsonSerializer.Deserialize<List<BitcoinOrdersDto>>(snapshot.AsksJson)!;
        if (bidsAgg.Count > Constants.OrdersShown)
            bidsAgg = bidsAgg[^Constants.OrdersShown..];
        if (asksAgg.Count > Constants.OrdersShown)
            asksAgg = asksAgg[^Constants.OrdersShown..];
        
        await clients.Client(connectionId).SendAsync("OrdersUpdate", new OrderBookSnapshotDto
        {
            Id = snapshot.Id,
            OpenAsksAgg = asksAgg,
            OpenBidsAgg = bidsAgg,
            UtcCreated = snapshot.UtcCreated
        });
    }

    /// <summary>
    /// Sends the most recent Bitcoin rate to a specific client.
    /// </summary>
    /// <param name="clients">The SignalR caller clients proxy.</param>
    /// <param name="connectionId">The SignalR connection ID of the requesting client.</param>
    public async Task ClientGetBitcoinRate(IHubCallerClients clients, string connectionId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<SqlContext>();
        var rate = await dbContext.BitcoinExchanges.OrderByDescending(x => x.Id).Select(x => x.BTCRate).FirstAsync();

        await clients.Client(connectionId).SendAsync("BtcRateUpdate", rate);
    }

    public async Task ClientGetBitcoinChart(IHubCallerClients clients, StockMarketSplitTypeEnum splitType, string connectionId)
    {
        using var scope = scopeFactory.CreateScope();
        var stockMarketService = scope.ServiceProvider.GetService<StockMarketService>();
        var chartData = await stockMarketService.GetBitcoinStockPrices(splitType);

        await clients.Client(connectionId).SendAsync("BitcoinChartUpdate", chartData);
    }

    public async Task SendBitcoinChartUpdate(IHubClients clients, StockMarketSplitTypeEnum splitType)
    {
        using var scope = scopeFactory.CreateScope();
        var stockMarketService = scope.ServiceProvider.GetService<StockMarketService>();
        var chartData = await stockMarketService.GetBitcoinStockPrices(splitType);

        await clients.All.SendAsync("BitcoinChartUpdate", chartData);
    }
}