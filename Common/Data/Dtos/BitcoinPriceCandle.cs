namespace Common.Data.Dtos
{
    public class BitcoinPriceCandle
    {
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Open { get; set; }
        public decimal Close { get; set; }
        public DateTime CandleStartUtc { get; set; }
    }
}
