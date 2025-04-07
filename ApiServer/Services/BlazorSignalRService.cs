using System.Text.Json;
using Common;
using Common.Data.Enums;
using Common.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Common.Data.Models;

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
    /// Sends the latest aggregated open bid and ask orders to a specific client.
    /// </summary>
    /// <param name="clients">The SignalR caller clients proxy.</param>
    /// <param name="connectionId">The SignalR connection ID of the requesting client.</param>
    public async Task ClientGetOrders(IHubCallerClients clients, int id, bool isRealTime, string connectionId)
    {
        using var scope = scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetService<OrdersService>();
        var snapshot = await orderService.GetOrderBookSnapshot(isRealTime, id);
        if (snapshot is not null)
        {
            await clients.Client(connectionId).SendAsync("OrdersUpdate", snapshot);
        }
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

    /// <summary>
    /// Sends Bitcoin chart data to a specific client based on the selected time interval split.
    /// </summary>
    /// <param name="clients">The SignalR clients interface.</param>
    /// <param name="splitType">The time interval to group chart data by (e.g., hourly, 15 minutes).</param>
    /// <param name="connectionId">The SignalR connection ID of the target client.</param>
    public async Task ClientGetBitcoinChart(IHubCallerClients clients, StockMarketSplitTypeEnum splitType, string connectionId)
    {
        using var scope = scopeFactory.CreateScope();
        var stockMarketService = scope.ServiceProvider.GetService<StockMarketService>();
        var chartData = await stockMarketService.GetBitcoinStockPrices(splitType);
        if (chartData is null)
            return;

        await clients.Client(connectionId).SendAsync("BitcoinChartUpdate", chartData);
    }

    /// <summary>
    /// Broadcasts updated Bitcoin chart data to all connected clients based on the selected time interval split.
    /// </summary>
    /// <param name="clients">The SignalR clients interface.</param>
    /// <param name="splitType">The time interval to group chart data by (e.g., hourly, 15 minutes).</param>
    public async Task SendBitcoinChartUpdate(IHubClients clients, StockMarketSplitTypeEnum splitType)
    {
        using var scope = scopeFactory.CreateScope();
        var stockMarketService = scope.ServiceProvider.GetService<StockMarketService>();
        var chartData = await stockMarketService.GetBitcoinStockPrices(splitType);
        if (chartData is null)
            return;

        await clients.All.SendAsync("BitcoinChartUpdate", chartData);
    }

    /// <summary>
    /// Sends a snapshot list of the current order book state to a specific client.
    /// </summary>
    /// <param name="clients">The SignalR clients interface.</param>
    /// <param name="connectionId">The SignalR connection ID of the target client.</param>
    public async Task ClientGetOrderBookSnapshots(IHubCallerClients clients, string connectionId)
    {
        using var scope = scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetService<OrdersService>();
        var snapshots = await orderService.GetSnapshotsSelectionTableDtos();

        await clients.Client(connectionId).SendAsync("OrderBookSnapshotsResponse", snapshots);
    }
}