using Common;
using Common.Data;
using Common.Data.Entities;
using Common.Dtos;
using Newtonsoft.Json.Linq;

namespace ApiServer.BackgroundWorkers
{
    public class BtcRatesFetcher : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly IConfiguration configuration;
        private readonly string url;
        private decimal lastRate;

        public BtcRatesFetcher(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            this.scopeFactory = scopeFactory;
            this.configuration = configuration;
            url = configuration.GetValue<string>("BtcRatesApi");
        }

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
                    await Task.Delay(Constants.CurrenciesFetcherDelayMs, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                // TODO: add logs
                //throw new Exception($"BtcRatesFetcher ex: {ex.Message}");
            }
        }

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
                await dbContext.BitcoinExchanges.AddAsync(new BitcoinExchange(dto.fifteenMin));
                await dbContext.SaveChangesAsync();
                lastRate = dto.fifteenMin;
            }
        }
    }
}
