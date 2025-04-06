using System.Threading.Channels;
using Common;
using Common.Data;
using Common.Data.Entities;
using Common.Data.Enums;
using Common.Data.Models;
using Newtonsoft.Json.Linq;

namespace ApiServer.BackgroundWorkers
{
    /// <summary>
    /// Background service that periodically fetches the current Bitcoin exchange rate from an external API,
    /// stores it in the database, and broadcasts updates to clients via SignalR if the rate has changed.
    /// </summary>
    public class BtcRatesFetcher : BackgroundService
    {
        private readonly ILogger<BtcRatesFetcher> logger;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ChannelWriter<EventDto> eventProceeder;
        private readonly IConfiguration configuration;
        private readonly string url;
        private decimal lastRate;

        /// <summary>
        /// Initializes a new instance of the <see cref="BtcRatesFetcher"/> class with dependencies for
        /// configuration, database access, and real-time communication.
        /// </summary>
        /// <param name="scopeFactory">Factory for creating service scopes for dependency resolution.</param>
        /// <param name="messageService">SignalR messaging service for broadcasting rate updates.</param>
        /// <param name="configuration">Application configuration object used to read API endpoint.</param>
        public BtcRatesFetcher(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory, ChannelWriter<EventDto> eventProceeder, IConfiguration configuration)
        {
            logger = loggerFactory.CreateLogger<BtcRatesFetcher>();
            this.scopeFactory = scopeFactory;
            this.eventProceeder = eventProceeder;
            this.configuration = configuration;
            url = configuration.GetValue<string>("BtcRatesApi");
        }

        /// <summary>
        /// Entry point for the background service. Continuously fetches Bitcoin rates on a scheduled interval
        /// until the service is cancelled. Only updates the database and clients if the rate has changed.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var scope = scopeFactory.CreateScope();
                await using var dbContext = scope.ServiceProvider.GetService<SqlContext>();
                lastRate = dbContext.BitcoinExchanges.OrderByDescending(x => x.Id).FirstOrDefault()?.BTCRate ?? 0m;
                var msDelay = (int)TimeSpan.FromSeconds(Constants.CurrenciesFetcherDelaySecs).TotalMilliseconds;

                while (!stoppingToken.IsCancellationRequested)
                {
                    await RunAsync(stoppingToken);
                    await Task.Delay(msDelay, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("BtcRatesFetcher ex: {exception}", ex);
            }
        }

        /// <summary>
        /// Executes a single fetch cycle from the external Bitcoin rates API. If a new rate is detected,
        /// stores it in the database and sends a real-time update to connected clients.
        /// </summary>
        private async Task RunAsync(CancellationToken stoppingToken)
        {
            var scope = scopeFactory.CreateScope();
            await using var dbContext = scope.ServiceProvider.GetService<SqlContext>();
            using var cli = new HttpClient();

            var response = await cli.GetAsync(url, stoppingToken);
            var text = await response.Content.ReadAsStringAsync(stoppingToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch BTC rates, msg: {errorMessage}", text);
            }
            
            var json = JObject.Parse(text);
            var dto = json.GetValue(Constants.FiatCurrency).ToObject<BtcRatesFetcherResponse>();

            if (lastRate != dto.fifteenMin)
            {
                var bitcoinExchange = new BitcoinExchange(dto.fifteenMin);
                await dbContext.BitcoinExchanges.AddAsync(bitcoinExchange, stoppingToken);
                await dbContext.SaveChangesAsync(stoppingToken);
                await eventProceeder.WriteAsync(new EventDto(EventTypeEnum.BitcoinRateChanged, DateTime.UtcNow, bitcoinExchange), stoppingToken);
                lastRate = dto.fifteenMin;
            }
        }
    }
}
