using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using PortfolioService.Data;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Services;

namespace PortfolioService.Consumers
{
    public class PriceUpdateRabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<PriceUpdateRabbitMqConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly PriceCache _priceCache;
        //private readonly PortfolioDbContext _dbContext;


        public PriceUpdateRabbitMqConsumer(ILogger<PriceUpdateRabbitMqConsumer> logger, IServiceProvider serviceProvider 
            ,PriceCache priceCache
            //,PortfolioDbContext dbContext
            )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _priceCache = priceCache;
            //_dbContext = dbContext;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                var factory = new ConnectionFactory { HostName = "localhost" };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "price-update-queue",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    // Log the received message (this can be replaced with actual processing logic)
                    _logger.LogInformation($"Received message: {message}");

                    // Deserialize the message (assuming the message is JSON)
                    var priceUpdate = JsonSerializer.Deserialize<PriceUpdate>(message);

                    // Handle the price update (e.g., store the price for order processing)
                    HandlePriceUpdate(priceUpdate);
                };

                channel.BasicConsume(queue: "price-update-queue",
                                     autoAck: true,
                                     consumer: consumer);

                // Keep the background service alive while listening for messages
                while (!stoppingToken.IsCancellationRequested)
                {
                    Task.Delay(1000, stoppingToken).Wait();
                }
            }, stoppingToken);
        }

        private void HandlePriceUpdate(PriceUpdate priceUpdate)
        {

            _priceCache.UpdatePrice(priceUpdate.Ticker, priceUpdate.Price);


            //var portfolios = await _dbContext.Portfolios.Include(p => p.StockHoldings).ToListAsync();

            ////TODO: refactor, getting all portfolios in memory and then updating them is not sufficent, will revist
            //foreach (var portfolio in portfolios)
            //{
            //    var stockHolding = portfolio.StockHoldings.FirstOrDefault(sh => sh.Ticker == message.Ticker);
            //    if (stockHolding != null)
            //    {
            //        // Update the total value based on the new price
            //        portfolio.TotalValue = stockHolding.Quantity * message.Price;
            //    }
            //}

            //await _dbContext.SaveChangesAsync();

            ////_priceCache.UpdatePrice(priceUpdate.Ticker, priceUpdate.Price);
            //_logger.LogInformation($"Processed price update for {priceUpdate.Ticker}: {priceUpdate.Price}");
        }
    }
}
