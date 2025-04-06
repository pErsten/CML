using Common;
using Common.Data;
using Common.Data.Enums;
using Common.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.Services;

public class StockMarketService
{
    private readonly ILogger<StockMarketService> logger;
    private readonly SqlContext dbContext;

    public StockMarketService(ILoggerFactory loggerFactory, SqlContext dbContext)
    {
        logger = loggerFactory.CreateLogger<StockMarketService>();
        this.dbContext = dbContext;
    }

    public async Task<List<BitcoinPriceCandle>?> GetBitcoinStockPrices(StockMarketSplitTypeEnum splitType)
    {
        try
        {
            var bitcoinPrices = await dbContext.BitcoinExchanges
                .Where(x => x.UtcDate > Constants.BitcoinPricesStartDate).AsNoTracking().ToListAsync();

            var split = splitType switch
            {
                StockMarketSplitTypeEnum.FifteenMins => (int)TimeSpan.FromMinutes(15).TotalSeconds,
                //StockMarketSplitTypeEnum.OneHour => (int)TimeSpan.FromHours(1).TotalSeconds,
                _ => throw new NotImplementedException()
            };

            var prevCloseVal = bitcoinPrices.First().BTCRate;
            return bitcoinPrices
                .GroupBy(x => ((int)(x.UtcDate - Constants.BitcoinPricesStartDate).TotalSeconds) / split)
                .Select(x =>
                {
                    var val = new BitcoinPriceCandle
                    {
                        Close = x.MaxBy(y => y.UtcDate).BTCRate,
                        Open = prevCloseVal,
                        High = x.Max(y => y.BTCRate),
                        Low = x.Min(y => y.BTCRate),
                        CandleStartUtc = Constants.BitcoinPricesStartDate.AddSeconds(x.Key * split)
                    };
                    prevCloseVal = val.Close;
                    return val;
                })
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError("GetBitcoinStockPrices ex: {exception}", ex);
            return null;
        }
    }
}