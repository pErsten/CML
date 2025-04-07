using Common.Data;
using Common.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace ApiServer.Tests
{
    public class TestFixture : IDisposable
    {
        public SqlContext DbContext { get; private set; }

        public TestFixture()
        {
            // TODO: figure out where to store string
            var options = new DbContextOptionsBuilder<SqlContext>()
                .UseInMemoryDatabase(databaseName: "TestDb") 
                .Options;

            DbContext = new SqlContext(options);


            var orderBookSnapshots = new List<OrderBookSnapshot>
            {
                new OrderBookSnapshot
                {
                    Id = 1,
                    BidsJson = "[{\"Price\": 10000, \"Amount\": 0.5}]",
                    AsksJson = "[{\"Price\": 11000, \"Amount\": 0.5}]",
                    UtcCreated = new DateTime(2025, 1, 1)
                },
                new OrderBookSnapshot
                {
                    Id = 2,
                    BidsJson = "[{\"Price\": 10000, \"Amount\": 0.5}]",
                    AsksJson = "[{\"Price\": 11000, \"Amount\": 0.5}]",
                    UtcCreated = new DateTime(2025, 1, 2)
                }
            };
            var bitcoinExchanges = new List<BitcoinExchange>
            {
                new BitcoinExchange
                {
                    Id = 1,
                    BTCRate = 50000m,
                    Currency = Constants.CryptoCurrency,
                    UtcDate = new DateTime(2025, 4, 7, 10, 0, 0, DateTimeKind.Utc)
                },
                new BitcoinExchange
                {
                    Id = 2,
                    BTCRate = 60000m,
                    Currency = Constants.CryptoCurrency,
                    UtcDate = new DateTime(2025, 4, 7, 11, 0, 0, DateTimeKind.Utc)
                },
                new BitcoinExchange
                {
                    Id = 3,
                    BTCRate = 70000m,
                    Currency = Constants.CryptoCurrency,
                    UtcDate = new DateTime(2025, 4, 7, 10, 2, 0, DateTimeKind.Utc)
                }
            };

            DbContext.BitcoinExchanges.AddRange(bitcoinExchanges);
            DbContext.OrderBookSnapshots.AddRange(orderBookSnapshots);

            DbContext.SaveChanges();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
        }
    }
}
