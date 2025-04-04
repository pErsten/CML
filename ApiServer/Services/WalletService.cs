using Common.Data;
using Common.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.Services
{
    /// <summary>
    /// Provides wallet-related operations such as retrieving or creating a wallet for an account and currency.
    /// </summary>
    public class WalletService
    {
        private readonly SqlContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletService"/> class.
        /// </summary>
        public WalletService(SqlContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves the existing wallet for a given account and currency, or creates a new wallet if one does not exist.
        /// </summary>
        /// <param name="accountId">The account ID to retrieve or create the wallet for.</param>
        /// <param name="currency">The currency type of the wallet (e.g., "USD", "BTC").</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the wallet.</returns>
        public async Task<AccountWallet> GetOrCreateWallet(int accountId, string currency)
            => await GetOrCreateWallet(dbContext, accountId, currency);

        /// <summary>
        /// Retrieves the existing wallet for a given account and currency, or creates a new wallet if one does not exist.
        /// </summary>
        /// <param name="dbContext">The database context used for accessing the wallet data.</param>
        /// <param name="accountId">The account ID to retrieve or create the wallet for.</param>
        /// <param name="currency">The currency type of the wallet (e.g., "USD", "BTC").</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the wallet.</returns>
        public static async Task<AccountWallet> GetOrCreateWallet(SqlContext dbContext, int accountId, string currency)
        {
            var wallet = await dbContext.AccountWallets.FirstOrDefaultAsync(x => x.AccountId == accountId && x.Currency == currency);
            if (wallet is null)
            {
                wallet = new AccountWallet(accountId, currency, 0);

                await dbContext.AccountWallets.AddAsync(wallet);
                await dbContext.SaveChangesAsync();
            }

            return wallet;
        }
    }
}
