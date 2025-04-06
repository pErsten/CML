using System.ComponentModel.DataAnnotations;

namespace Common.Data.Entities
{
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
