using System.Threading.Channels;
using ApiServer.Services;
using Common;
using Common.Data;
using Common.Data.Entities;
using Common.Data.Enums;
using Common.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.BackgroundWorkers
{
    public class OrdersManager: BackgroundService
    {
        private readonly ChannelReader<BitcoinOrder> channelReader;
        private readonly IServiceScopeFactory scopeFactory;
        private List<BitcoinOrder> openBids;
        private List<BitcoinOrder> openAsks;
        private Dictionary<int, AccountWallet> fiatWallets;
        private Dictionary<int, AccountWallet> cryptoWallets;
        private BitcoinOrder? highestBid;
        private BitcoinOrder? smallestAsk;

        public OrdersManager(ChannelReader<BitcoinOrder> channelReader, IServiceScopeFactory scopeFactory)
        {
            this.channelReader = channelReader;
            this.scopeFactory = scopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var updatedWalletsOfAccs = new List<AccountWallet>();
                using (var scope = scopeFactory.CreateScope())
                {
                    await using (var dbContext = scope.ServiceProvider.GetService<SqlContext>())
                    {
                        var openOrders = await dbContext.BitcoinOrders.Where(x => x.Status == OrderStatusEnum.Open).AsNoTracking().ToListAsync(stoppingToken);
                        openBids = openOrders.Where(x => x.Type == OrderTypeEnum.Bid).OrderBy(x => x.BtcPrice).ToList(); // Highest price in the end to manipulate easier
                        openAsks = openOrders.Where(x => x.Type == OrderTypeEnum.Ask).OrderByDescending(x => x.BtcPrice).ToList(); // Lowest price in the end
                        highestBid = openBids.LastOrDefault();
                        smallestAsk = openAsks.LastOrDefault();
                        var bidAccs = openBids.Select(x => x.AccountId).Distinct();
                        var askAccs = openAsks.Select(x => x.AccountId).Distinct();
                        fiatWallets = await dbContext.AccountWallets
                            .Where(x => x.Currency == Constants.FiatCurrency && bidAccs.Contains(x.AccountId))
                            .AsNoTracking()
                            .ToDictionaryAsync(x => x.AccountId, stoppingToken);
                        cryptoWallets = await dbContext.AccountWallets
                            .Where(x => x.Currency == Constants.CryptoCurrency && askAccs.Contains(x.AccountId))
                            .AsNoTracking()
                            .ToDictionaryAsync(x => x.AccountId, stoppingToken);
                        dbContext.ChangeTracker.Clear();
                    }
                }
                
                await foreach (var order in channelReader.ReadAllAsync(stoppingToken))
                {
                    order.Account = null;
                    using var scope = scopeFactory.CreateScope();
                    switch (order.Type)
                    {
                        case OrderTypeEnum.Bid:
                            BidInsert(order);
                            if (highestBid is null || order.BtcPrice > highestBid.BtcPrice)
                            {
                                highestBid = order;
                            }
                            break;
                        case OrderTypeEnum.Ask:
                            AskInsert(order);
                            if (smallestAsk is null || order.BtcPrice < smallestAsk.BtcPrice)
                            {
                                smallestAsk = order;
                            }
                            break;
                        default:
                            // TODO: add logger
                            continue;
                    }

                    var dbContext = scope.ServiceProvider.GetService<SqlContext>();
                    var walletService = scope.ServiceProvider.GetService<WalletService>();
                    var hubService = scope.ServiceProvider.GetService<SignalRService>();
                    var isChanged = false;
                    while (highestBid?.BtcPrice >= smallestAsk?.BtcPrice)
                    {
                        if (highestBid is null || smallestAsk is null)
                        {
                            break;
                        }

                        dbContext.Attach(highestBid);
                        dbContext.Attach(smallestAsk);
                        if (!fiatWallets.TryGetValue(highestBid.AccountId, out var fiatBidderWallet) ||
                            fiatBidderWallet.Amount < highestBid.BtcRemained * highestBid.BtcPrice)
                        {
                            var fiatAmount = fiatBidderWallet?.Amount ?? 0m;
                            var userBids = openBids.Where(x =>
                                x.AccountId == highestBid.AccountId && fiatAmount < x.BtcRemained * x.BtcPrice).ToList();
                            foreach (var userBid in userBids)
                            {
                                userBid.Status = userBid.BtcAmount > userBid.BtcRemained
                                    ? OrderStatusEnum.PartiallyCancelled
                                    : OrderStatusEnum.Cancelled;
                                userBid.UtcUpdated = DateTime.UtcNow;
                                openBids.Remove(userBid);
                            }

                            highestBid = openBids.LastOrDefault();
                            await dbContext.SaveChangesAsync(stoppingToken);
                            continue;
                        }

                        if (!cryptoWallets.TryGetValue(smallestAsk.AccountId, out var cryptoAskerWallet) ||
                            cryptoAskerWallet.Amount < smallestAsk.BtcRemained)
                        {
                            var cryptoAmount = cryptoAskerWallet?.Amount ?? 0m;
                            var userAsks = openAsks.Where(x =>
                                x.AccountId == smallestAsk.AccountId && x.BtcRemained > cryptoAmount);
                            foreach (var userAsk in userAsks)
                            {
                                userAsk.Status = userAsk.BtcAmount > userAsk.BtcRemained
                                    ? OrderStatusEnum.PartiallyCancelled
                                    : OrderStatusEnum.Cancelled;
                                userAsk.UtcUpdated = DateTime.UtcNow;
                                openAsks.Remove(userAsk);
                            }

                            smallestAsk = openAsks.LastOrDefault();
                            await dbContext.SaveChangesAsync(stoppingToken);
                            continue;
                        }

                        var btcAmount = Math.Min(highestBid.BtcRemained, smallestAsk.BtcRemained);
                        var btcPrice = highestBid.BtcPrice;
                        highestBid.BtcRemained -= btcAmount;
                        smallestAsk.BtcRemained -= btcAmount;
                        highestBid.UtcUpdated = DateTime.UtcNow;
                        smallestAsk.UtcUpdated = DateTime.UtcNow;
                        var trans = new BitcoinOrderTransaction(highestBid.Id, smallestAsk.Id, btcAmount, btcPrice);
                        if (highestBid.BtcRemained <= 0)
                        {
                            highestBid.Status = OrderStatusEnum.Filled;
                            openBids.Remove(highestBid);
                            highestBid = openBids.LastOrDefault();
                        }

                        if (smallestAsk.BtcRemained <= 0)
                        {
                            smallestAsk.Status = OrderStatusEnum.Filled;
                            openAsks.Remove(smallestAsk);
                            smallestAsk = openAsks.LastOrDefault();
                        }

                        fiatBidderWallet =
                            await WalletService.GetOrCreateWallet(dbContext, fiatBidderWallet.AccountId, Constants.FiatCurrency);
                        cryptoAskerWallet =
                            await WalletService.GetOrCreateWallet(dbContext, cryptoAskerWallet.AccountId, Constants.CryptoCurrency);
                        var fiatAskerWallet =
                            await WalletService.GetOrCreateWallet(dbContext, cryptoAskerWallet.AccountId, Constants.FiatCurrency);
                        var cryptoBidderWallet =
                            await WalletService.GetOrCreateWallet(dbContext, fiatBidderWallet.AccountId, Constants.CryptoCurrency);
                        fiatBidderWallet.Amount -= btcAmount * btcPrice;
                        cryptoBidderWallet.Amount += btcAmount;
                        cryptoAskerWallet.Amount -= btcAmount;
                        fiatAskerWallet.Amount += btcAmount * btcPrice;
                        updatedWalletsOfAccs.Add(cryptoBidderWallet);
                        updatedWalletsOfAccs.Add(cryptoAskerWallet);
                        updatedWalletsOfAccs.Add(fiatBidderWallet);
                        updatedWalletsOfAccs.Add(fiatAskerWallet);

                        await dbContext.BitcoinOrderTransactions.AddAsync(trans, stoppingToken);
                        isChanged = true;
                    }

                    var messagesHub = scope.ServiceProvider.GetService<IHubContext<SignalRHub>>();
                    var bidsAgg = Enumerable.Range(0, Constants.OrdersShown).Select(x => new BitcoinOrdersDto())
                        .ToList();
                    var asksAgg = Enumerable.Range(0, Constants.OrdersShown).Select(x => new BitcoinOrdersDto())
                        .ToList();
                    for (int i = 0, j = openBids.Count - 1; j >= 0; j--)
                    {
                        if (bidsAgg[i].Price != 0 && bidsAgg[i].Price != openBids[j].BtcPrice)
                        {
                            i++;
                            if (i >= Constants.OrdersShown)
                            {
                                break;
                            }
                        }

                        bidsAgg[i].Price = openBids[j].BtcPrice;
                        bidsAgg[i].Amount += openBids[j].BtcRemained;
                    }

                    for (int i = 0, j = openAsks.Count - 1; j >= 0; j--)
                    {
                        if (asksAgg[i].Price != 0 && asksAgg[i].Price != openAsks[j].BtcPrice)
                        {
                            i++;
                            if (i >= Constants.OrdersShown)
                            {
                                break;
                            }
                        }

                        asksAgg[i].Price = openAsks[j].BtcPrice;
                        asksAgg[i].Amount += openAsks[j].BtcRemained;
                    }

                    await hubService.SendOrdersUpdate(messagesHub.Clients, bidsAgg, asksAgg);

                    if (isChanged)
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);

                        var accIds = updatedWalletsOfAccs.Select(x => x.AccountId).Distinct();
                        var accountLogins = await dbContext.Accounts.Where(x => accIds.Contains(x.Id))
                            .ToDictionaryAsync(x => x.Id, x => x.Login);
                        var tasks = updatedWalletsOfAccs.Distinct().Select(x =>
                            hubService.SendWalletUpdate(messagesHub.Clients, accountLogins[x.AccountId],
                                new AccountWalletDto(x)));
                        await Task.WhenAll(tasks);
                        updatedWalletsOfAccs.Clear();
                        dbContext.ChangeTracker.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"shit - {ex}");
            }
        }


        private Comparer<BitcoinOrder> bidComparer =
            Comparer<BitcoinOrder>.Create((x, y) => Math.Sign(x.BtcPrice - y.BtcPrice));
        private Comparer<BitcoinOrder> askComparer =
            Comparer<BitcoinOrder>.Create((x, y) => Math.Sign(y.BtcPrice - x.BtcPrice));
        private void BidInsert(BitcoinOrder order)
        {
            var index = openBids.BinarySearch(order, bidComparer);
            openBids.Insert(index < 0 ? ~index : index, order);
        }
        private void AskInsert(BitcoinOrder order)
        {
            var index = openAsks.BinarySearch(order, askComparer);
            openAsks.Insert(index < 0 ? ~index : index, order);
        }
    }
}
