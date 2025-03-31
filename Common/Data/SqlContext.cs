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


        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<BitcoinExchange>().HasIndex(x => new { x.UtcDate, x.Currency });
            mb.Entity<BitcoinExchange>().Property(x => x.BTCRate).HasPrecision(18, 2);

            base.OnModelCreating(mb);
        }
    }
}
