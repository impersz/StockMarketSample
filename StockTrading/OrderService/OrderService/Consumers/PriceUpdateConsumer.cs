using MassTransit;
using OrderService.Services;
using System.Threading.Tasks;

namespace OrderService.Consumers
{
    public class PriceUpdateConsumer : IConsumer<PriceUpdate>
    {
        private readonly PriceCache _priceCache;

        public PriceUpdateConsumer(PriceCache priceCache)
        {
            _priceCache = priceCache;
        }

        public Task Consume(ConsumeContext<PriceUpdate> context)
        {
            var priceUpdate = context.Message;
            _priceCache.UpdatePrice(priceUpdate.Ticker, priceUpdate.Price);
            return Task.CompletedTask;
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
