using System.Text.Json;
using System.Threading.Channels;
using ApiServer.Controllers;
using ApiServer.Services;
using Common.Data;
using Common.Data.Dtos;
using Common.Data.Entities;
using Common.Data.Enums;
using Microsoft.AspNetCore.SignalR;

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
                switch (newEvent.EventType, newEvent.EventBody)
                {
                    case (EventTypeEnum.BitcoinRateChanged, BitcoinExchange exchange):
                        await signalRService.SendBitcoinRateUpdate(signalRHub.Clients, exchange.BTCRate);
                        var json = JsonSerializer.Serialize(exchange);
                        await dbContext.Events.AddAsync(new AppEvent(newEvent.UtcCreated, json), stoppingToken);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        break;
                    case (EventTypeEnum.OrderBookUpdated, _):
                        // TODO: add stuff
                        break;
                }
            }
        }
    }
}
