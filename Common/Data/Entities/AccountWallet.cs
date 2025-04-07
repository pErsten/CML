using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Data.Entities
{
    /// <summary>
    /// Represents a wallet tied to a specific user account, containing a currency type and its associated balance.
    /// </summary>
    public class AccountWallet
    {
        [Key]
        public int Id { get; set; }
        public int AccountId { get; set; }
        [ForeignKey("AccountId")]
        public Account Account { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }

        public AccountWallet() { }
        public AccountWallet(int accountId, string currency, decimal amount)
        {
            AccountId = accountId;
            Currency = currency;
            Amount = amount;
        }
    }
}
