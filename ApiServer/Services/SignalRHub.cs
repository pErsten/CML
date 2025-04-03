using Common;
using Common.Data;
using Common.Data.Entities;
using Common.Data.Enums;
using Common.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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

    public class SignalRService
    {
        private readonly IServiceScopeFactory scopeFactory;

        public SignalRService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

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

        public async Task SendBitcoinRateUpdate(IHubClients clients, decimal newRate)
        {
            await clients.All.SendAsync("BtcRateUpdate", newRate);
        }

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

        public async Task ClientGetOrders(IHubCallerClients clients, string connectionId)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<SqlContext>();
            var bidsAgg = await dbContext.BitcoinOrders
                .Where(x => x.Status == OrderStatusEnum.Open && x.Type == OrderTypeEnum.Bid)
                .GroupBy(x => x.BtcPrice)
                .Select(x => new BitcoinOrdersDto
                {
                    Price = x.Key,
                    Amount = x.Sum(y => y.BtcRemained)
                }).OrderByDescending(x => x.Price).Take(Constants.OrdersShown).ToListAsync();
            var asksAgg = await dbContext.BitcoinOrders
                .Where(x => x.Status == OrderStatusEnum.Open && x.Type == OrderTypeEnum.Ask)
                .GroupBy(x => x.BtcPrice)
                .Select(x => new BitcoinOrdersDto
                {
                    Price = x.Key,
                    Amount = x.Sum(y => y.BtcRemained)
                }).OrderBy(x => x.Price).Take(Constants.OrdersShown).ToListAsync();


            await clients.Client(connectionId).SendAsync("OrdersUpdate", bidsAgg, asksAgg);
        }

        public async Task ClientGetBitcoinRate(IHubCallerClients clients, string connectionId)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<SqlContext>();
            var rate = await dbContext.BitcoinExchanges.OrderByDescending(x => x.Id).Select(x => x.BTCRate).FirstAsync();

            await clients.Client(connectionId).SendAsync("BtcRateUpdate", rate);
        }
    }
}
