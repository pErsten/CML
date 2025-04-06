using System.Security.Cryptography;
using System.Text;
using Common.Data;
using Common.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiServer.Services
{
    /// <summary>
    /// Provides authentication-related functionality, including user registration,
    /// login validation, and password hashing.
    /// </summary>
    public class AuthService
    {
        private ILogger<AuthService> logger;
        private readonly SqlContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        public AuthService(ILoggerFactory loggerFactory, SqlContext dbContext)
        {
            logger = loggerFactory.CreateLogger<AuthService>();
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves an existing account from the database by login.
        /// </summary>
        /// <param name="login">The login name to search for.</param>
        /// <returns>The matching <see cref="Account"/> if found; otherwise, null.</returns>
        private async Task<Account?> GetExistingUser(string login)
        {
            return await dbContext.Accounts.FirstOrDefaultAsync(x => x.Login == login);
        }

        /// <summary>
        /// Registers a new user or returns the existing one if already registered with the same password.
        /// </summary>
        /// <param name="login">The login name of the new user.</param>
        /// <param name="passwordHash">The hashed password to store.</param>
        /// <returns>
        /// The newly created or existing <see cref="Account"/> if the password matches;
        /// otherwise, null if login exists with a different password.
        /// </returns>
        public async Task<Account> AddNewUser(string login, string passwordHash)
        {
            var account = await GetExistingUser(login);
            if (account is not null)
            {
                logger.LogWarning("Tried to register already existing user: {userGuid}", account.AccountId);
                return account.PasswordHash != passwordHash! ? null! : account;
            }

            account = new Account(passwordHash, login);
            await dbContext.Accounts.AddAsync(account);
            await dbContext.SaveChangesAsync();
            return account;
        }

        /// <summary>
        /// Validates a user's login credentials.
        /// </summary>
        /// <param name="login">The user's login name.</param>
        /// <param name="passwordHash">The hashed password to validate.</param>
        /// <returns>
        /// The <see cref="Account"/> if the credentials are valid; otherwise, null.
        /// </returns>
        public async Task<Account?> ValidateUser(string login, string passwordHash)
        {
            var account = await GetExistingUser(login);
            if (account is null)
            {
                logger.LogWarning("User doesn't exist, login: {login}", login);
                return null;
            }

            if (passwordHash != account.PasswordHash)
            {
                logger.LogInformation("Wrong password, login: {login}", login);
                return null;
            }

            return account;
        }

        /// <summary>
        /// Hashes a plain-text password using SHA-256 and returns the result as a hex string.
        /// </summary>
        /// <param name="password">The plain-text password.</param>
        /// <returns>The hashed password as an uppercase hexadecimal string.</returns>
        public static string PasswordHasher(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
