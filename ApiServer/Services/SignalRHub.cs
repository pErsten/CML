using Common;
using Common.Data.Entities;
using Common.Dtos;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace ApiServer.Services
{
    public class SignalRHub : Hub
    {
        private readonly SignalRService service;
        /*public async Task SendUserWalletUpdateMessages(List<int> accIds)
        {
            var tasks = accIds.Select(x => Clients.All.SendAsync("WalletUpdate", x));
            await Task.WhenAll(tasks);
        }

        public async Task SendOrdersUpdate(List<BitcoinOrder> openBids, List<BitcoinOrder> openAsks)
        {
            await Clients.All.SendAsync("OrdersUpdate", openBids[^Constants.OrdersShown..], openAsks[^Constants.OrdersShown..]);
        }
        public async Task SendOrdersUpdate(List<BitcoinOrdersDto> openBids, List<BitcoinOrdersDto> openAsks)
        {
            await Clients.All.SendAsync("OrdersUpdate", openBids, openAsks);
        }*/

        public SignalRHub(SignalRService service)
        {
            this.service = service;
        }

        public async Task SubscribeToWalletUpdates(string username)
        {
            await service.SubscribeToWalletUpdates(username, Context.ConnectionId);
        }
    }

    public class SignalRService
    {

        private Dictionary<string, string> _subscriptions = new Dictionary<string, string>();

        public async Task SubscribeToWalletUpdates(string username, string connectionId)
        {
            _subscriptions[username] = connectionId;
        }
        public async Task SendWalletUpdate(IHubClients clients, string username, AccountWalletDto wallet)
        {
            if (_subscriptions.TryGetValue(username, out var connectionId))
            {
                await clients.Client(connectionId).SendAsync("WalletUpdate", wallet);
            }
        }
        public async Task SendOrdersUpdate(IHubClients clients, List<BitcoinOrdersDto> bidsAgg, List<BitcoinOrdersDto> asksAgg)
        {
            await clients.All.SendAsync("OrdersUpdate", bidsAgg, asksAgg);
        }

    }
}
