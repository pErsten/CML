namespace Common.Data.Dtos
{
    public class OrderBookSnapshotDto
    {
        public int Id { get; set; }
        public DateTime UtcCreated { get; set; }
        public List<BitcoinOrdersDto> OpenAsksAgg { get; set; }
        public List<BitcoinOrdersDto> OpenBidsAgg { get; set; }
    }
}
