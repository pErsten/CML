using System.ComponentModel.DataAnnotations;

namespace Common.Data.Entities
{
    public class BitcoinExchange
    {
        [Key]
        public int Id { get; set; }
        public decimal BTCRate { get; set; }
        public string Currency { get; set; }
        public DateTime UtcDate { get; set; }

        public BitcoinExchange() {}
        public BitcoinExchange(decimal rate)
        {
            BTCRate = rate;
            Currency = Constants.DefaultCurrency;
            UtcDate = DateTime.UtcNow;
        }
    }
}