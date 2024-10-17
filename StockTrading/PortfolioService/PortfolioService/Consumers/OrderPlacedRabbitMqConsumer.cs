using PortfolioService.Data;
using PortfolioService.Services;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PortfolioService.Consumers
{
    public class OrderPlacedRabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<OrderPlacedRabbitMqConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        //private readonly PortfolioDbContext _dbContext;

        public OrderPlacedRabbitMqConsumer(ILogger<OrderPlacedRabbitMqConsumer> logger, IServiceProvider serviceProvider
            //,PortfolioDbContext dbContext
            )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            //_dbContext = dbContext;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {

                var factory = new ConnectionFactory { HostName = "localhost" };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "order-placed-queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    // Log the received message (this can be replaced with actual processing logic)
                    _logger.LogInformation($"Received message: {message}");

                    // Deserialize the message (assuming the message is JSON)
                    var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(message);

                    // Handle the order placed - update the portfolio data
                    await HandleOrderPlacedAsync(orderPlaced);
                };

                channel.BasicConsume(queue: "order-placed-queue",
                                     autoAck: true,
                                     consumer: consumer);

                // Keep the background service alive while listening for messages
                while (!stoppingToken.IsCancellationRequested)
                {
                    Task.Delay(1000, stoppingToken).Wait();
                }
            }, stoppingToken);
        }

        private async Task HandleOrderPlacedAsync(OrderPlacedEvent message)
        {
            // Create a scope to resolve DbContext
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();


            var portfolio = await dbContext.Portfolios.Include(p => p.StockHoldings).FirstOrDefaultAsync(p => p.UserId == message.UserId);

            if (portfolio == null)
            {
                if (message.Side.ToLower() == "buy")
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

                    dbContext.Portfolios.Add(portfolio);
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
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
