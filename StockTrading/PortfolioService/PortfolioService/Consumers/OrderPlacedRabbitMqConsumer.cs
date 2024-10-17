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

        public OrderPlacedRabbitMqConsumer(ILogger<OrderPlacedRabbitMqConsumer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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

                    _logger.LogInformation($"Received message: {message}");

                    var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(message);

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

            //TODO: Move logic to separate methods for Buying and Selling, make more readable

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

    //TODO: Move the OrderPlacedEvent to SharedModels

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
