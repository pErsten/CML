using ApiServer.Services;
using Common.Data;
using Common.Data.Entities;
using Common.Data.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApiServer.Tests.UnitTests;

public class StockMarketServiceUnitTests : IClassFixture<TestFixture>
{
    private readonly StockMarketService service;
    private readonly SqlContext inMemoryDb;

    public StockMarketServiceUnitTests(TestFixture testFixture)
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger<OrdersService>().Returns(Substitute.For<ILogger<OrdersService>>());

        inMemoryDb = testFixture.DbContext;
        service = new StockMarketService(loggerFactory, inMemoryDb);
    }

    [Fact]
    public async Task GetBitcoinStockPrices_ShouldReturnCandlestickData_WhenDataExists()
    {
        // Arrange
        var type = StockMarketSplitTypeEnum.FifteenMins;
        var expectedResult = 2;

        // Act
        var result = await service.GetBitcoinStockPrices(type);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(result.Count, expectedResult);
    }
}