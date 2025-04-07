using System.Globalization;
using System.Text.Json;
using System.Threading.Channels;
using Common;
using Common.Data;
using Common.Data.Entities;
using Common.Data.Enums;
using Common.Data.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ApiServer.BackgroundWorkers
{
    public class OrderBookFetcher : BackgroundService
    {
        private readonly ILogger<OrderBookFetcher> logger;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ChannelWriter<EventDto> eventProceeder;
        private readonly IConfiguration configuration;
        private readonly string url;

        public OrderBookFetcher(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, ChannelWriter<EventDto> eventProceeder, IConfiguration configuration)
        {
            logger = loggerFactory.CreateLogger<OrderBookFetcher>();
            this.scopeFactory = scopeFactory;
            this.eventProceeder = eventProceeder;
            this.configuration = configuration;
            url = configuration.GetValue<string>("BitstampOrderBookSnapshotUrl");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var msDelay = (int)TimeSpan.FromSeconds(Constants.OrderBookSnapshotFetcherDelaySecs).TotalMilliseconds;

                while (!stoppingToken.IsCancellationRequested)
                {
                    await RunAsync(stoppingToken);
                    await Task.Delay(msDelay, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("OrderBookFetcher ex: {exception}", ex);
            }
        }

        private async Task RunAsync(CancellationToken stoppingToken)
        {
            var scope = scopeFactory.CreateScope();
            await using var dbContext = scope.ServiceProvider.GetService<SqlContext>();
            using var cli = new HttpClient();

            var response = await cli.GetAsync(url, stoppingToken);
            var text = await response.Content.ReadAsStringAsync(stoppingToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch order book snapshot, msg: {errorMessage}", text);
            }

            var json = JsonSerializer.Deserialize<OrderBookSnapshotApiResp>(text);
            var dateTimeUtc = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(json.timestamp)).DateTime;
            var aggAmount = 0m;
            var asksAgg = json.asks.GroupBy(x => Convert.ToInt32(x[0], CultureInfo.InvariantCulture)).Select(x => new BitcoinOrdersDto
            {
                Price = x.Key,
                Amount = x.Sum(y => Convert.ToDecimal(y[1], CultureInfo.InvariantCulture))
            }).OrderBy(x => x.Price)
            .Select(x =>
            {
                x.Amount += aggAmount;
                aggAmount = x.Amount;
                return x;
            }).ToList();
            aggAmount = 0m;
            var bidsAgg = json.bids.GroupBy(x => Convert.ToInt32(x[0], CultureInfo.InvariantCulture)).Select(x => new BitcoinOrdersDto
            {
                Price = x.Key,
                Amount = x.Sum(y => Convert.ToDecimal(y[1], CultureInfo.InvariantCulture))
            }).OrderByDescending(x => x.Price)
            .Select(x =>
            {
                x.Amount += aggAmount;
                aggAmount = x.Amount;
                return x;
            }).ToList();
            
            var orderBookSnapshot = new OrderBookSnapshot(dateTimeUtc, JsonSerializer.Serialize(asksAgg), JsonSerializer.Serialize(bidsAgg));
            await dbContext.OrderBookSnapshots.AddAsync(orderBookSnapshot, stoppingToken);
            await dbContext.SaveChangesAsync(stoppingToken);

            await eventProceeder.WriteAsync(new EventDto(EventTypeEnum.OrderBookUpdated, DateTime.UtcNow, new OrderBookSnapshotDto
            {
                Id = orderBookSnapshot.Id,
                OpenAsksAgg = asksAgg[..Constants.OrdersShown],
                OpenBidsAgg = bidsAgg[..Constants.OrdersShown],
                IsRealTime = true,
                UtcCreated = dateTimeUtc
            }), stoppingToken);
        }
    }
}
