using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace PortfolioService.Data
{
    public class PortfolioDbContext : DbContext
    {
        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options)
        {
        }

        public DbSet<Portfolio> Portfolios { get; set; }
        public DbSet<StockHolding> StockHoldings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define the one-to-many relationship between Portfolio and StockHolding
            modelBuilder.Entity<Portfolio>()
                .HasMany(p => p.StockHoldings)
                .WithOne(sh => sh.Portfolio)
                .HasForeignKey(sh => sh.PortfolioId);
        }
    }

}
