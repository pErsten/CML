using System.Threading.Channels;
using Common.Data;
using Common.Data.Entities;
using Common.Data.Enums;

namespace ApiServer.BackgroundWorkers
{
    public class OrdersManager: BackgroundService
    {
        private readonly ChannelReader<BitcoinOrder> channelReader;
        private readonly IServiceScopeFactory scopeFactory;
        private List<BitcoinOrder> openBids;
        private List<BitcoinOrder> openAsks;
        private BitcoinOrder? highestBid;
        private BitcoinOrder? smallestAsk;

        public OrdersManager(ChannelReader<BitcoinOrder> channelReader, IServiceScopeFactory scopeFactory)
        {
            this.channelReader = channelReader;
            this.scopeFactory = scopeFactory;

            using var scope = scopeFactory.CreateScope();
            using var dbContext = scope.ServiceProvider.GetService<SqlContext>();
            var openOrders = dbContext.BitcoinOrders.Where(x => x.Status == OrderStatusEnum.Open).ToList();
            openBids = openOrders.Where(x => x.Type == OrderTypeEnum.Bid).OrderBy(x => x.BtcPrice).ToList(); // Highest price in the end to manipulate easier
            openAsks = openOrders.Where(x => x.Type == OrderTypeEnum.Ask).OrderByDescending(x => x.BtcPrice).ToList(); // Lowest price in the end
            highestBid = openBids.LastOrDefault();
            smallestAsk = openAsks.LastOrDefault();
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = scopeFactory.CreateScope();
            await foreach (var order in channelReader.ReadAllAsync(stoppingToken))
            {
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
                var isChanged = false;
                while (highestBid?.BtcPrice > smallestAsk?.BtcPrice)
                {
                    if (highestBid is null || smallestAsk is null)
                    {
                        break;
                    }
                    var amount = Math.Min(highestBid.BtcRemained, smallestAsk.BtcRemained);
                    var btcPrice = highestBid.BtcPrice;
                    highestBid.BtcRemained -= amount;
                    smallestAsk.BtcRemained -= amount;
                    highestBid.UtcUpdated = DateTime.UtcNow;
                    smallestAsk.UtcUpdated = DateTime.UtcNow;
                    var trans = new BitcoinOrderTransaction(highestBid.Id, smallestAsk.Id, amount, btcPrice);
                    if (highestBid.BtcAmount <= 0)
                    {
                        highestBid.Status = OrderStatusEnum.Filled;
                        openBids.Remove(highestBid);
                        highestBid = openBids.LastOrDefault();
                    }
                    if (smallestAsk.BtcAmount <= 0)
                    {
                        smallestAsk.Status = OrderStatusEnum.Filled;
                        openAsks.Remove(smallestAsk);
                        smallestAsk = openAsks.LastOrDefault();
                    }
                    // TODO: change user balances

                    await dbContext.BitcoinOrderTransactions.AddAsync(trans, stoppingToken);
                    isChanged = true;
                }

                if (isChanged)
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
        }



        //  TODO: check signs
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
