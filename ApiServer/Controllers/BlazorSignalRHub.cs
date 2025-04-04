using ApiServer.Services;
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
        /// <param name="service">Service responsible for managing client data retrieval and updates.</param>

        public BlazorSignalRHub(BlazorSignalRService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Allows an authenticated client to subscribe to real-time wallet balance updates.
        /// Maps the client's SignalR connection ID to their account ID.
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
        /// Sends the current wallet balance to the authenticated client based on their account ID.
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
        /// Sends the current Bitcoin exchange rate to the connected client.
        /// </summary>
        public async Task ClientGetBitcoinRate()
        {
            await service.ClientGetBitcoinRate(Clients, Context.ConnectionId);
        }

        /// <summary>
        /// Sends the current order book (open bids and asks) to the connected client.
        /// </summary>
        public async Task ClientGetOrders()
        {
            await service.ClientGetOrders(Clients, Context.ConnectionId);
        }
    }
}
