using Common.Data;
using Common.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.Services
{
    public class WalletService
    {
        private readonly SqlContext dbContext;

        public WalletService(SqlContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<AccountWallet> GetOrCreateWallet(int accountId, string currency)
            => await GetOrCreateWallet(dbContext, accountId, currency);
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
