using System.ComponentModel.DataAnnotations;

namespace Common.Data.Entities
{
    /// <summary>
    /// Represents a snapshot of the order book at a specific point in time, including serialized data for asks and bids.
    /// </summary>
    public class OrderBookSnapshot
    {
        [Key]
        public int Id { get; set; }
        public DateTime UtcCreated { get; set; }
        public string AsksJson { get; set; }
        public string BidsJson { get; set; }

        public OrderBookSnapshot() { }
        public OrderBookSnapshot(DateTime utcCreated, string asksJson, string bidsJson)
        {
            UtcCreated = utcCreated;
            AsksJson = asksJson;
            BidsJson = bidsJson;
        }
    }
}
