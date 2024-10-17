using MassTransit;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;

namespace PortfolioService.Consumers
{
    public class PriceUpdateConsumer : IConsumer<PriceUpdate>
    {
        private readonly PortfolioDbContext _dbContext;

        public PriceUpdateConsumer(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<PriceUpdate> context)
        {
            var message = context.Message;
            var portfolios = await _dbContext.Portfolios.Include(p => p.StockHoldings).ToListAsync();
            
            //TODO: refactor, getting all portfolios in memory and then updating them is not sufficent, will revist
            foreach (var portfolio in portfolios)
            {
                var stockHolding = portfolio.StockHoldings.FirstOrDefault(sh => sh.Ticker == message.Ticker);
                if (stockHolding != null)
                {
                    // Update the total value based on the new price
                    portfolio.TotalValue = stockHolding.Quantity * message.Price;
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }

    #region Temporary
    //Temporary solution to save time. Would otherwise create a separate class library project for shared models
    public class PriceUpdate
    {
        public DateTime Timestamp { get; set; } // Timestamp of the price update

        public required string Ticker { get; set; } // Stock ticker (e.g., AAPL)

        public decimal Price { get; set; } // New stock price        
    }
    #endregion
}
