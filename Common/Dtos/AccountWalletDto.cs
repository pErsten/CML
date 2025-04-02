using Common.Data.Entities;

namespace Common.Dtos
{
    public class AccountWalletDto
    {
        public int AccountId { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }

        public AccountWalletDto() { }
        public AccountWalletDto(AccountWallet wallet)
        {
            this.AccountId = wallet.AccountId;
            this.Currency = wallet.Currency;
            this.Amount = wallet.Amount;
        }
    }
}
