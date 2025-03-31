using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using Common.Data;
using Common.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.Services
{
    public class AuthService
    {
        private readonly SqlContext dbContext;

        public AuthService(SqlContext dbContext)
        {
            this.dbContext = dbContext;
        }

        private async Task<Account?> GetExistingUser(string login)
        {
            return await dbContext.Accounts.FirstOrDefaultAsync(x => x.Login == login);
        }

        public async Task<Account> AddNewUser(string login, string passwordHash)
        {
            var account = await GetExistingUser(login);
            if (account is not null)
            {
                // TODO: add logger
                //logger.LogError("User already exists")
                if (account.PasswordHash != passwordHash)
                    return null;
                return account;
            }

            account = new Account(passwordHash, login);
            await dbContext.Accounts.AddAsync(account);
            await dbContext.SaveChangesAsync();
            return account;
        }

        public async Task<Account?> ValidateUser(string login, string passwordHash)
        {
            var account = await GetExistingUser(login);
            if (account is null)
            {
                // TODO: add logger
                //logger.LogError("User doesn't exist")
                return null;
            }

            if (passwordHash != account.PasswordHash)
            {
                // TODO: add logger
                //logger.LogError("Wrong password")
                return null;
            }

            return account;
        }

        public static string PasswordHasher(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
