using MassTransit;
using System;
using System.Threading.Tasks;

namespace PriceService
{
    public class PricePublisher
    {
        private readonly IBus _bus;

        public PricePublisher(IBus bus)
        {
            _bus = bus;
        }

        public async Task PublishPriceUpdateAsync(string ticker, decimal price)
        {
            var priceUpdate = new PriceUpdate
            {
                Timestamp = DateTime.UtcNow,
                Ticker = ticker,
                Price = price
            };

            try
            {
                // Publish the price update to RabbitMQ
                await _bus.Publish(priceUpdate);

                // Log the price update (optional for debugging)
                Console.WriteLine($"Published price update: {ticker} - {price}");
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"EXCEPTION on publishing: {ticker} - {price}");
            }

           
        }
    }
}
