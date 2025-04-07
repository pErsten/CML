using Common;
using Common.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Common.Data.Models;

namespace ApiServer.Services
{
    /// <summary>
    /// Service responsible for retrieving order book snapshot data from the database.
    /// </summary>
    public class OrdersService
    {
        private readonly ILogger<OrdersService> logger;
        private readonly SqlContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersService"/> class with logging and database access.
        /// </summary>
        public OrdersService(ILoggerFactory loggerFactory, SqlContext dbContext)
        {
            logger = loggerFactory.CreateLogger<OrdersService>();
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves a single order book snapshot, either the latest (real-time) or a historical one by ID.
        /// </summary>
        /// <param name="isRealTime">Indicates whether to fetch the most recent snapshot.</param>
        /// <param name="id">The ID of the snapshot to fetch if <paramref name="isRealTime"/> is false.</param>
        /// <returns>An <see cref="OrderBookSnapshotDto"/> representing the snapshot, or null if not found.</returns>
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
                    bidsAgg = bidsAgg[..Constants.OrdersShown];
                if (asksAgg.Count > Constants.OrdersShown)
                    asksAgg = asksAgg[..Constants.OrdersShown];

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

        /// <summary>
        /// Retrieves a list of snapshot metadata for display in a selection table.
        /// </summary>
        /// <returns>A list of <see cref="OrderBookSnapshotSelectionTableDto"/> objects.</returns>
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
