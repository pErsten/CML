using ApiServer.Services;
using Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApiServer.Tests.UnitTests
{
    public class OrdersServiceUnitTests : IClassFixture<TestFixture>
    {
        private OrdersService service;
        private SqlContext inMemoryDb;
        public OrdersServiceUnitTests(TestFixture testFixture)
        {
            var loggerFactory = Substitute.For<ILoggerFactory>();
            loggerFactory.CreateLogger<OrdersService>().Returns(Substitute.For<ILogger<OrdersService>>());

            inMemoryDb = testFixture.DbContext;
            service = new OrdersService(loggerFactory, inMemoryDb);
        }

        [Fact]
        public async Task GetSnapshotsSelectionTableDtos_NotEmpty()
        {
            // Assign
            var expectedResult = await inMemoryDb.OrderBookSnapshots.CountAsync();

            // Act
            var result = await service.GetSnapshotsSelectionTableDtos();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(result.Count, expectedResult);
        }

        [Fact]
        public async Task GetOrderBookSnapshot_ShouldReturnNull_WhenNonExistingId()
        {
            // Assign
            var id = 3;

            // Act
            var result = await service.GetOrderBookSnapshot(false, id);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetOrderBookSnapshot_ShouldReturnData_WhenExistingId()
        {
            // Assign
            var id = 1;

            // Act
            var result = await service.GetOrderBookSnapshot(false, id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Id, id);
        }

        [Fact]
        public async Task GetOrderBookSnapshot_ShouldReturnData_WhenTheMostRecentValue()
        {
            // Assign
            var id = 0;
            var expectedId = inMemoryDb.OrderBookSnapshots.OrderByDescending(x => x.UtcCreated).First().Id;

            // Act
            var result = await service.GetOrderBookSnapshot(true, id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result.Id, expectedId);
        }
    }
}