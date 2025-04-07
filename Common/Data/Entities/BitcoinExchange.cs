using System.ComponentModel.DataAnnotations;

namespace Common.Data.Entities
{
    /// <summary>
    /// Represents a historical Bitcoin exchange rate record, including the rate, currency, and timestamp.
    /// </summary>
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
            Currency = Constants.FiatCurrency;
            UtcDate = DateTime.UtcNow;
        }
    }
}