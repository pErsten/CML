using Common.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Common.Data
{
    public class SqlContext : DbContext
    {
        public SqlContext(DbContextOptions<SqlContext> options) : base(options)
        {
            
        }
        public DbSet<BitcoinExchange> BitcoinExchanges { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AppEvent> Events { get; set; }
        public DbSet<OrderBookSnapshot> OrderBookSnapshots { get; set; }


        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<BitcoinExchange>().HasIndex(x => new { x.UtcDate, x.Currency });
            mb.Entity<BitcoinExchange>().Property(x => x.BTCRate).HasPrecision(18, 2);

            mb.Entity<Account>().Property(x => x.Login).HasMaxLength(100);
            mb.Entity<Account>().Property(x => x.AccountId).HasMaxLength(50);
            mb.Entity<Account>().HasIndex(x => x.Login).IsUnique();
            mb.Entity<Account>().HasIndex(x => x.AccountId).IsUnique();

            base.OnModelCreating(mb);
        }
    }
}
