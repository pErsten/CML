using Common;
using Common.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Common.Data.Models;

namespace ApiServer.Services
{
    public class OrdersService
    {
        private readonly ILogger<OrdersService> logger;
        private readonly SqlContext dbContext;

        public OrdersService(ILoggerFactory loggerFactory, SqlContext dbContext)
        {
            logger = loggerFactory.CreateLogger<OrdersService>();
            this.dbContext = dbContext;
        }

        public async Task<OrderBookSnapshotDto?> GetOrderBookSnapshot(bool isRealTime, int id)
        {
            try
            {
                var snapshot = isRealTime
                    ? await dbContext.OrderBookSnapshots.OrderByDescending(x => x.Id).AsNoTracking()
                        .FirstOrDefaultAsync()
                    : await dbContext.OrderBookSnapshots.Where(x => x.Id == id).AsNoTracking().FirstOrDefaultAsync();
                if (snapshot is null)
                    return null;
                var bidsAgg = JsonSerializer.Deserialize<List<BitcoinOrdersDto>>(snapshot.BidsJson)!;
                var asksAgg = JsonSerializer.Deserialize<List<BitcoinOrdersDto>>(snapshot.AsksJson)!;
                if (bidsAgg.Count > Constants.OrdersShown)
                    bidsAgg = bidsAgg[^Constants.OrdersShown..];
                if (asksAgg.Count > Constants.OrdersShown)
                    asksAgg = asksAgg[^Constants.OrdersShown..];

                return new OrderBookSnapshotDto
                {
                    Id = snapshot.Id,
                    OpenAsksAgg = asksAgg,
                    OpenBidsAgg = bidsAgg,
                    UtcCreated = snapshot.UtcCreated,
                    IsRealTime = isRealTime
                };
            }
            catch (Exception ex)
            {
                logger.LogError("GetOrderBookSnapshot ex: {exception}", ex);
                return null;
            }
        }

        public async Task<List<OrderBookSnapshotSelectionTableDto>> GetSnapshotsSelectionTableDtos()
        {
            return await dbContext.OrderBookSnapshots.Select(x => new OrderBookSnapshotSelectionTableDto
            {
                Id = x.Id,
                UtcCreated = x.UtcCreated
            }).ToListAsync();
        }
    }
}
