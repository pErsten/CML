using Common;
using Common.Data.Enums;
using Common.Data;
using Common.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.Services;

public class BlazorSignalRService
{
    private readonly IServiceScopeFactory scopeFactory;

    public BlazorSignalRService(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    private Dictionary<string, string> _subscriptions = new Dictionary<string, string>();

    public async Task SubscribeToWalletUpdates(string accountGuid, string connectionId)
    {
        _subscriptions[accountGuid] = connectionId;
    }
    public async Task SendWalletUpdate(IHubClients clients, string accountGuid, AccountWalletDto wallet)
    {
        if (_subscriptions.TryGetValue(accountGuid, out var connectionId))
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