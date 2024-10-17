using MassTransit;
using MassTransit.Transports;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;

namespace PortfolioService.Consumers
{
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly PortfolioDbContext _dbContext;

        public OrderPlacedConsumer(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var message = context.Message;
            var portfolio = await _dbContext.Portfolios.Include(p => p.StockHoldings).FirstOrDefaultAsync(p => p.UserId == message.UserId);

            if (portfolio == null)
            {
                if(message.Side.ToLower() == "buy")
                {
                    // Create new portfolio if it doesn't exist
                    portfolio = new Portfolio
                    {
                        UserId = message.UserId,
                        StockHoldings = new List<StockHolding>
                        {
                            new StockHolding
                            {
                                Ticker = message.Ticker,
                                Quantity = message.Quantity
                            }
                        },
                        TotalValue = message.Quantity * message.Price
                    };

                    _dbContext.Portfolios.Add(portfolio);
                }                
            }
            else //User already has a portfolio
            {
                if (message.Side.ToLower() == "buy")
                {
                    // Find if the user already has this stock in their holdings
                    var stockHolding = portfolio.StockHoldings.FirstOrDefault(sh => sh.Ticker == message.Ticker);

                    if (stockHolding == null) // No stocks and buying -> add stocks
                    {
                        stockHolding = new StockHolding
                        {
                            Ticker = message.Ticker,
                            Quantity = message.Quantity,
                        };

                        portfolio.StockHoldings.Add(stockHolding);
                    }
                    else // Have stocks and buying -> Add the Quantity
                    {
                        // Update the quantity and price for an existing stock holding
                        stockHolding.Quantity += message.Quantity;
                        //stockHolding.Price = price; // Update to latest price
                        
                        // Recalculate portfolio value               
                        portfolio.TotalValue += message.Quantity * message.Price;
                    }
                }
                else if (message.Side == "sell")
                {
                    // Find if the user already has this stock in their holdings
                    var stockHolding = portfolio.StockHoldings.FirstOrDefault(sh => sh.Ticker == message.Ticker);

                    //No stocks and selling -> error
                    if (stockHolding == null)
                    {
                        throw new InvalidOperationException($"Cannot sell {message.Quantity} shares of {message.Ticker} because the user does not own any.");
                    }
                    else // Have stocks and selling -> Extract the Quantity
                    {
                        // Update the quantity and price for an existing stock holding
                        stockHolding.Quantity -= message.Quantity;
                        if (stockHolding.Quantity >= 0)
                        {
                            // Recalculate portfolio value               
                            portfolio.TotalValue -= message.Quantity * message.Price;

                            //Even at 0 quantity will still keep the record for now.
                        }
                        else 
                        {
                            throw new InvalidOperationException($"Cannot sell {message.Quantity} shares of {message.Ticker} because the user only owns {stockHolding.Quantity}.");
                        }
                    }

                }


                //// Update existing portfolio
                //if (portfolio.StockHoldings.ContainsKey(message.Ticker))
                //{
                //    portfolio.StockHoldings[message.Ticker] += message.Quantity;
                //}
                //else
                //{
                //    portfolio.StockHoldings[message.Ticker] = message.Quantity;
                //}

                //// Recalculate portfolio value               
                //portfolio.TotalValue += message.Quantity * message.Price;
            }

            await _dbContext.SaveChangesAsync();
        }
    }

    #region Temporary
    //Temporary solution to save time. Would otherwise create a separate class library project for shared models
    public class OrderPlacedEvent
    {
        public required string UserId { get; set; }
        public required string Ticker { get; set; }
        public int Quantity { get; set; }
        public required string Side { get; set; }
        public decimal Price { get; set; }
        public DateTime OrderDate { get; set; }
    }
    #endregion
}
