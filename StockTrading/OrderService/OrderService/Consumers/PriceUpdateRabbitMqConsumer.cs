using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using OrderService.Services;
using SharedModels;

namespace OrderService.Consumers
{
    public class PriceUpdateRabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<PriceUpdateRabbitMqConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly PriceCache _priceCache;

        private IConnection _connection;
        private IModel _channel;

        public PriceUpdateRabbitMqConsumer(ILogger<PriceUpdateRabbitMqConsumer> logger, IServiceProvider serviceProvider, PriceCache priceCache)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _priceCache = priceCache;

            // Initialize RabbitMQ connection and channel in constructor (to reuse the connection)
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Prefetch 10 messages at a time to handle traffic efficiently
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

            // Declare the queue
            _channel.QueueDeclare(queue: "price-update-queue",
                                  durable: true,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                var consumer = new EventingBasicConsumer(_channel);

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation($"Received message: {message}");

                    try
                    {
                        var priceUpdate = JsonSerializer.Deserialize<PriceUpdate>(message);

                        HandlePriceUpdate(priceUpdate);

                        // Acknowledge the message only after successful processing
                        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        // Log the error and potentially requeue the message or move to DLQ
                        _logger.LogError(ex, $"Error processing message: {message}");

                        // Optionally Nack or requeue the message based on the failure
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                    }
                };

                _channel.BasicConsume(queue: "price-update-queue",
                                     autoAck: false,  // Ensure manual acknowledgement
                                     consumer: consumer);

                // Keep the background service alive while listening for messages
                while (!stoppingToken.IsCancellationRequested)
                {
                    Task.Delay(1000, stoppingToken).Wait();
                }

                DisposeConnection();

            }, stoppingToken);
        }

        private void HandlePriceUpdate(PriceUpdate priceUpdate)
        {
            _priceCache.UpdatePrice(priceUpdate.Ticker, priceUpdate.Price);
            _logger.LogInformation($"Processed price update for {priceUpdate.Ticker}: {priceUpdate.Price}");
        }

        private void DisposeConnection()
        {
            // Dispose connection and channel when the service is stopping
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        public override void Dispose()
        {
            DisposeConnection();
            base.Dispose();
        }
    }
}
