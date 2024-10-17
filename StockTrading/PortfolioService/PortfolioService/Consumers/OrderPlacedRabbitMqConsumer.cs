using PortfolioService.Data;
using PortfolioService.Services;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Connections;
using System.Threading.Channels;

namespace PortfolioService.Consumers
{
    public class OrderPlacedRabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<OrderPlacedRabbitMqConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConnectionFactory _connectionFactory;

        public OrderPlacedRabbitMqConsumer(ILogger<OrderPlacedRabbitMqConsumer> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            // Ensure the factory uses persistent connections
            _connectionFactory = new ConnectionFactory
            {
                HostName = "localhost",
                DispatchConsumersAsync = true // Allow async consumer methods
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "order-placed-queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    try
                    {
                        _logger.LogInformation($"Received message: {message}");

                        var orderPlaced = JsonSerializer.Deserialize<OrderPlacedEvent>(message);

                        await HandleOrderPlacedAsync(orderPlaced);

                        // Acknowledge the message only after successful processing
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex) 
                    {
                        // Log the error and potentially requeue the message or move to DLQ
                        _logger.LogError(ex, $"Error processing message: {message}");

                        // Optionally Nack or requeue the message based on the failure
                        channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                   
                };

                channel.BasicConsume(queue: "order-placed-queue",
                                     autoAck: false,  // Ensure manual acknowledgement
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
                // Find if the user already has this stock in their holdings
                var stockHolding = portfolio.StockHoldings.FirstOrDefault(sh => sh.Ticker == message.Ticker);

                if (message.Side.ToLower() == "buy")
                {
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
                    }

                    // Recalculate portfolio value               
                    portfolio.TotalValue += message.Quantity * message.Price;
                }
                else if (message.Side.ToLower() == "sell")
                {
                    //No stocks and selling -> error
                    if (stockHolding == null)
                    {
                        throw new InvalidOperationException($"Cannot sell {message.Quantity} shares of {message.Ticker} because the user does not own any.");
                    }
                    else // Have stocks and selling -> Extract the Quantity
                    {
                        if(stockHolding.Quantity < message.Quantity)
                        {
                            throw new InvalidOperationException($"Cannot sell {message.Quantity} shares of {message.Ticker} because the user only owns {stockHolding.Quantity}.");
                        }

                        // Update the quantity and price for an existing stock holding
                        stockHolding.Quantity -= message.Quantity;

                        // Recalculate portfolio value               
                        portfolio.TotalValue -= message.Quantity * message.Price;
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
