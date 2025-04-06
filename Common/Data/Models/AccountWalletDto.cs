using Common.Data.Entities;

namespace Common.Data.Models
{
    public class AccountWalletDto
    {
        public int AccountId { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }

        public AccountWalletDto() { }
        public AccountWalletDto(AccountWallet wallet)
        {
            AccountId = wallet.AccountId;
            Currency = wallet.Currency;
            Amount = wallet.Amount;
        }
    }
}
