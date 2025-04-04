using ApiServer.Controllers;
using ApiServer.Services;
using Common;
using Common.Data;
using Common.Data.Entities;
using Common.Dtos;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace ApiServer.BackgroundWorkers
{
    /// <summary>
    /// Background service that periodically fetches the current Bitcoin exchange rate from an external API,
    /// stores it in the database, and broadcasts updates to clients via SignalR if the rate has changed.
    /// </summary>
    public class BtcRatesFetcher : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly BlazorSignalRService messageService;
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
        public BtcRatesFetcher(IServiceScopeFactory scopeFactory, BlazorSignalRService messageService, IConfiguration configuration)
        {
            this.scopeFactory = scopeFactory;
            this.messageService = messageService;
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

                while (!stoppingToken.IsCancellationRequested)
                {
                    await RunAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(Constants.CurrenciesFetcherDelaySecs).Milliseconds, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                // TODO: add logs
                //throw new Exception($"BtcRatesFetcher ex: {ex.Message}");
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
            if (!response.IsSuccessStatusCode)
            {
                // TODO: add logs
                throw new Exception("Failed to fetch BTC rates");
            }
            
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var dto = json.GetValue("USD").ToObject<BtcRatesFetcherResponse>();

            if (lastRate != dto.fifteenMin)
            {
                var messageHub = scope.ServiceProvider.GetService<IHubContext<BlazorSignalRHub>>();
                messageService.SendBitcoinRateUpdate(messageHub.Clients, dto.fifteenMin);
                await dbContext.BitcoinExchanges.AddAsync(new BitcoinExchange(dto.fifteenMin));
                await dbContext.SaveChangesAsync();
                lastRate = dto.fifteenMin;
            }
        }
    }
}
