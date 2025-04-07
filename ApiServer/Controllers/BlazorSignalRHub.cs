using ApiServer.Services;
using Common.Data.Enums;
using Microsoft.AspNetCore.SignalR;

namespace ApiServer.Controllers
{
    /// <summary>
    /// SignalR hub used by Blazor WebAssembly clients to subscribe to real-time updates
    /// such as wallet balances, Bitcoin rates, and active orders.
    /// </summary>
    public class BlazorSignalRHub : Hub
    {
        private readonly BlazorSignalRService service;

        /// <summary>
        /// Initializes the SignalR hub with the provided SignalR service.
        /// </summary>
        /// <param name="service">Service responsible for managing real-time updates and client communication.</param>
        public BlazorSignalRHub(BlazorSignalRService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Allows an authenticated client to subscribe to real-time wallet updates.
        /// Associates the client's SignalR connection ID with their account.
        /// </summary>
        public async Task SubscribeToWalletUpdates()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                return;
            }
            var accountGuid = Context.User.Identity.Name;
            await service.SubscribeToWalletUpdates(accountGuid, Context.ConnectionId);
        }

        /// <summary>
        /// Sends the current wallet balances (fiat and crypto) to the authenticated client.
        /// </summary>
        public async Task ClientGetUserBalance()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                return;
            }
            var accountGuid = Context.User.Identity.Name;
            await service.ClientGetUserBalance(Clients, Context.ConnectionId, accountGuid);
        }

        /// <summary>
        /// Sends the current Bitcoin exchange rate to the client.
        /// </summary>
        public async Task ClientGetBitcoinRate()
        {
            await service.ClientGetBitcoinRate(Clients, Context.ConnectionId);
        }

        /// <summary>
        /// Sends a Bitcoin chart (split by the specified interval) to the client.
        /// </summary>
        /// <param name="splitType">The time grouping to use for chart data (e.g., by hour, 15 minutes, etc.).</param>
        public async Task ClientGetBitcoinChart(StockMarketSplitTypeEnum splitType)
        {
            await service.ClientGetBitcoinChart(Clients, splitType, Context.ConnectionId);
        }

        /// <summary>
        /// Sends order data to the client. If no ID is provided, it will return the most recent order book.
        /// </summary>
        /// <param name="id">Optional account ID to fetch specific user's orders.</param>
        public async Task ClientGetOrders(int? id)
        {
            await service.ClientGetOrders(Clients, id ?? 0, id is null, Context.ConnectionId);
        }

        /// <summary>
        /// Sends a snapshot of the current order book (aggregated bids and asks) to the client.
        /// </summary>
        public async Task ClientGetOrderBookSnapshots()
        {
            await service.ClientGetOrderBookSnapshots(Clients, Context.ConnectionId);
        }
    }
}
