using ApiServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace ApiServer.Controllers
{
    public class BlazorSignalRHub : Hub
    {
        private readonly BlazorSignalRService service;
        public BlazorSignalRHub(BlazorSignalRService service)
        {
            this.service = service;
        }

        public async Task SubscribeToWalletUpdates()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                return;
            }
            var accountGuid = Context.User.Identity.Name;
            await service.SubscribeToWalletUpdates(accountGuid, Context.ConnectionId);
        }

        public async Task ClientGetUserBalance()
        {
            if (!Context.User.Identity.IsAuthenticated)
            {
                return;
            }
            var accountGuid = Context.User.Identity.Name;
            await service.ClientGetUserBalance(Clients, Context.ConnectionId, accountGuid);
        }

        public async Task ClientGetBitcoinRate()
        {
            await service.ClientGetBitcoinRate(Clients, Context.ConnectionId);
        }

        public async Task ClientGetOrders()
        {
            await service.ClientGetOrders(Clients, Context.ConnectionId);
        }
    }
}
