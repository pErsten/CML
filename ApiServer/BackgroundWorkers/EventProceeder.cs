using System.Text.Json;
using System.Threading.Channels;
using ApiServer.Controllers;
using ApiServer.Services;
using Common.Data;
using Common.Data.Dtos;
using Common.Data.Entities;
using Common.Data.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.BackgroundWorkers
{
    public class EventProceeder : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ChannelReader<EventDto> eventsChannel;
        private readonly BlazorSignalRService signalRService;

        public EventProceeder(IServiceScopeFactory scopeFactory, ChannelReader<EventDto> eventsChannel, BlazorSignalRService signalRService)
        {
            this.scopeFactory = scopeFactory;
            this.eventsChannel = eventsChannel;
            this.signalRService = signalRService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var newEvent in eventsChannel.ReadAllAsync(stoppingToken))
            {
                var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<SqlContext>();
                var signalRHub = scope.ServiceProvider.GetService<IHubContext<BlazorSignalRHub>>();
                string json = string.Empty;
                switch (newEvent.EventType, newEvent.EventBody)
                {
                    case (EventTypeEnum.BitcoinRateChanged, BitcoinExchange exchange):
                        await signalRService.SendBitcoinRateUpdate(signalRHub.Clients, exchange.BTCRate);
                        await signalRService.SendBitcoinChartUpdate(signalRHub.Clients, StockMarketSplitTypeEnum.FifteenMins);

                        json = JsonSerializer.Serialize(exchange);
                        await dbContext.Events.AddAsync(new AppEvent(newEvent, json), stoppingToken);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        break;
                    case (EventTypeEnum.OrderBookUpdated, OrderBookSnapshotDto snapshot):
                        await signalRService.SendOrdersUpdate(signalRHub.Clients, snapshot);

                        json = JsonSerializer.Serialize(new { snapshot.Id, snapshot.UtcCreated });
                        await dbContext.Events.AddAsync(new AppEvent(newEvent, json), stoppingToken);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        break;
                    case (EventTypeEnum.WalletBalancesChanged, List<AccountWalletDto> wallets):
                        var accIds = wallets.Select(x => x.AccountId).Distinct();
                        var accountGuids = await dbContext.Accounts.Where(x => accIds.Contains(x.Id))
                            .ToDictionaryAsync(x => x.Id, x => x.AccountId, stoppingToken);
                        var tasks = wallets.Distinct().Select(x =>
                            signalRService.SendWalletUpdate(signalRHub.Clients, accountGuids[x.AccountId], x));
                        await Task.WhenAll(tasks);

                        json = JsonSerializer.Serialize(wallets);
                        await dbContext.Events.AddAsync(new AppEvent(newEvent, json), stoppingToken);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        break;
                }
            }
        }
    }
}
