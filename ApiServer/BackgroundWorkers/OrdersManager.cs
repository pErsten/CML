﻿using System.Threading.Channels;
using ApiServer.Controllers;
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
    /// <summary>
    /// Background service responsible for processing real-time Bitcoin orders (bids and asks).
    /// 
    /// It consumes new orders from an asynchronous channel, maintains live lists of open bids and asks,
    /// performs automatic order matching, updates wallets and order statuses, stores transactions,
    /// and pushes updates to web clients via SignalR.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersManager"/> class with a channel for incoming orders
        /// and a scope factory to manage service lifetimes.
        /// </summary>
        /// <param name="channelReader">The channel reader used to receive new Bitcoin orders.</param>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency resolution of scoped and transient services.</param>
        public OrdersManager(ChannelReader<BitcoinOrder> channelReader, IServiceScopeFactory scopeFactory)
        {
            this.channelReader = channelReader;
            this.scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Main loop of the background service that continuously reads new orders, processes order matching,
        /// manages wallet balances, stores transactions, and notifies clients via SignalR.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var updatedWalletsOfAccs = new List<AccountWallet>();
            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] OrdersManager initialization err: {ex}");
            }

            await foreach (var order in channelReader.ReadAllAsync(stoppingToken))
            {
                try
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
                    var hubService = scope.ServiceProvider.GetService<BlazorSignalRService>();
                    var isChanged = false;
                    while (highestBid is not null && smallestAsk is not null &&
                           highestBid.BtcPrice >= smallestAsk.BtcPrice)
                    {
                        dbContext.Attach(highestBid);
                        dbContext.Attach(smallestAsk);
                        if (!fiatWallets.TryGetValue(highestBid.AccountId, out var fiatBidderWallet) ||
                            fiatBidderWallet.Amount < highestBid.BtcRemained * highestBid.BtcPrice)
                        {
                            var fiatAmount = fiatBidderWallet?.Amount ?? 0m;
                            var userBids = openBids.Where(x =>
                                    x.AccountId == highestBid.AccountId && fiatAmount < x.BtcRemained * x.BtcPrice)
                                .ToList();
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
                            await WalletService.GetOrCreateWallet(dbContext, fiatBidderWallet.AccountId,
                                Constants.FiatCurrency);
                        cryptoAskerWallet =
                            await WalletService.GetOrCreateWallet(dbContext, cryptoAskerWallet.AccountId,
                                Constants.CryptoCurrency);
                        var fiatAskerWallet =
                            await WalletService.GetOrCreateWallet(dbContext, cryptoAskerWallet.AccountId,
                                Constants.FiatCurrency);
                        var cryptoBidderWallet =
                            await WalletService.GetOrCreateWallet(dbContext, fiatBidderWallet.AccountId,
                                Constants.CryptoCurrency);
                        fiatBidderWallet.Amount -= btcAmount * btcPrice;
                        cryptoBidderWallet.Amount += btcAmount;
                        cryptoAskerWallet.Amount -= btcAmount;
                        fiatAskerWallet.Amount += btcAmount * btcPrice;
                        updatedWalletsOfAccs.Add(cryptoBidderWallet);
                        updatedWalletsOfAccs.Add(cryptoAskerWallet);

                        await dbContext.BitcoinOrderTransactions.AddAsync(trans, stoppingToken);
                        isChanged = true;
                    }

                    var messagesHub = scope.ServiceProvider.GetService<IHubContext<BlazorSignalRHub>>();

                    var bidsAgg = AggregateOrders(Constants.OrdersShown, openBids);
                    var asksAgg = AggregateOrders(Constants.OrdersShown, openAsks);

                    await hubService.SendOrdersUpdate(messagesHub.Clients, bidsAgg, asksAgg);

                    if (isChanged)
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);

                        var accIds = updatedWalletsOfAccs.Select(x => x.AccountId).Distinct();
                        var accountGuids = await dbContext.Accounts.Where(x => accIds.Contains(x.Id))
                            .ToDictionaryAsync(x => x.Id, x => x.AccountId, stoppingToken);
                        var tasks = updatedWalletsOfAccs.Distinct().Select(x =>
                            hubService.SendWalletUpdate(messagesHub.Clients, accountGuids[x.AccountId],
                                new AccountWalletDto(x)));
                        await Task.WhenAll(tasks);
                        updatedWalletsOfAccs.Clear();
                        dbContext.ChangeTracker.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] OrdersManager iteration on order {order.Id} err: {ex}");
                }
            }
        }

        /// <summary>
        /// Aggregates the top N open orders by bitcoin price level, grouping remaining amounts for display or broadcasting.
        /// </summary>
        /// <param name="listSize">The maximum number of price levels to aggregate.</param>
        /// <param name="openOrders">The list of open orders to aggregate (bids or asks).</param>
        /// <returns>A list of aggregated order DTOs.</returns>
        private List<BitcoinOrdersDto> AggregateOrders(int listSize, List<BitcoinOrder> openOrders)
        {
            var ordersAgg = Enumerable
                .Range(0, Constants.OrdersShown)
                .Select(x => new BitcoinOrdersDto())
                .ToList();

            for (int i = 0, j = openOrders.Count - 1; j >= 0; j--)
            {
                if (ordersAgg[i].Price != 0 && ordersAgg[i].Price != openOrders[j].BtcPrice)
                {
                    i++;
                    if (i >= listSize)
                    {
                        break;
                    }
                }

                ordersAgg[i].Price = openOrders[j].BtcPrice;
                ordersAgg[i].Amount += openOrders[j].BtcRemained;
            }

            return ordersAgg;
        }


        private Comparer<BitcoinOrder> bidComparer =
            Comparer<BitcoinOrder>.Create((x, y) => Math.Sign(x.BtcPrice - y.BtcPrice));
        private Comparer<BitcoinOrder> askComparer =
            Comparer<BitcoinOrder>.Create((x, y) => Math.Sign(y.BtcPrice - x.BtcPrice));

        /// <summary>
        /// Inserts a new bid order into the list of open bids, maintaining ascending order by price.
        /// </summary>
        /// <param name="order">The bid order to insert.</param>
        private void BidInsert(BitcoinOrder order)
        {
            var index = openBids.BinarySearch(order, bidComparer);
            openBids.Insert(index < 0 ? ~index : index, order);
        }

        /// <summary>
        /// Inserts a new ask order into the list of open asks, maintaining descending order by price.
        /// </summary>
        /// <param name="order">The ask order to insert.</param>
        private void AskInsert(BitcoinOrder order)
        {
            var index = openAsks.BinarySearch(order, askComparer);
            openAsks.Insert(index < 0 ? ~index : index, order);
        }
    }
}
