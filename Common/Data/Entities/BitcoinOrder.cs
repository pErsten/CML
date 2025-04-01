using System.ComponentModel.DataAnnotations.Schema;
using Common.Data.Enums;

namespace Common.Data.Entities
{
    public class BitcoinOrder
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        [ForeignKey("AccountId")]
        public Account Account { get; set; }
        public OrderTypeEnum Type { get; set; }
        public OrderStatusEnum Status { get; set; }
        public decimal BtcAmount { get; set; }
        public decimal BtcRemained { get; set; }
        public decimal BtcPrice { get; set; }
        public DateTime UtcCreated { get; set; }
        public DateTime UtcUpdated { get; set; }

        public BitcoinOrder() { }
        public BitcoinOrder(int accountId, OrderTypeEnum type, decimal btcAmount, decimal btcPrice)
        {
            AccountId = accountId;
            Type = type;
            BtcAmount = btcAmount;
            BtcRemained = btcAmount;
            BtcPrice = btcPrice;
            Status = OrderStatusEnum.Open;
            UtcCreated = DateTime.UtcNow;
            UtcUpdated = UtcCreated;
        }
    }
}
