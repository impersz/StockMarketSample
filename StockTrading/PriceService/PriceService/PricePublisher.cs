using MassTransit;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PriceService
{
    public class PricePublisher
    {
        public PricePublisher()
        {
            
        }

        public void PublishPriceUpdate(string ticker, decimal price)
        {
            var priceUpdate = new PriceUpdate
            {
                Timestamp = DateTime.UtcNow,
                Ticker = ticker,
                Price = price
            };

            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "price-update-queue",
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);
            
            // Serialize object to JSON string
            string jsonString = JsonSerializer.Serialize(priceUpdate);
            // Convert JSON string to byte array
            byte[] body = Encoding.UTF8.GetBytes(jsonString);

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: "price-update-queue",
                                 basicProperties: null,
                                 body: body);

            Console.WriteLine($"Published price update: {ticker} - {price}");
        }
    }
}
