using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Data.Entities
{
    /// <summary>
    /// Represents a transaction that occurs between two Bitcoin orders (bid and ask), including the amount, price, and creation time.
    /// </summary>
    public class BitcoinOrderTransaction
    {
        [Key]
        public int Id { get; set; }

        public int BidOrderId { get; set; }
        [ForeignKey("BidOrderId")]
        public BitcoinOrder BidOrder { get; set; }

        public int AskOrderId { get; set; }
        [ForeignKey("AskOrderId")]
        public BitcoinOrder AskOrder { get; set; }

        public decimal BtcAmount { get; set; }
        public decimal BtcPrice { get; set; }
        public DateTime UtcCreated { get; set; }

        public BitcoinOrderTransaction() { }
        public BitcoinOrderTransaction(int bidOrderId, int askOrderId, decimal btcAmount, decimal btcPrice)
        {
            BidOrderId = bidOrderId;
            AskOrderId = askOrderId;
            BtcAmount = btcAmount;
            BtcPrice = btcPrice;
            UtcCreated = DateTime.UtcNow;
        }
    }
}
