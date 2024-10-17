using System.Collections.Concurrent;

namespace OrderService.Services
{
    public class PriceCache
    {
        private readonly ConcurrentDictionary<string, decimal> _latestPrices = new ConcurrentDictionary<string, decimal>();

        public void UpdatePrice(string ticker, decimal price)
        {
            _latestPrices[ticker] = price;
        }

        public decimal GetPrice(string ticker)
        {
            return _latestPrices.TryGetValue(ticker, out var price) ? price : 1m;
        }
    }
}
