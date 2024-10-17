namespace PriceService
{
    public class PriceGenerator : BackgroundService
    {
        private readonly Dictionary<string, decimal> _stockPrices;
        private readonly Random _random;
        private readonly PricePublisher _pricePublisher;
        private readonly ILogger<PriceGenerator> _logger;

        public PriceGenerator(PricePublisher pricePublisher, ILogger<PriceGenerator> logger)
        {
            _pricePublisher = pricePublisher;
            _random = new Random();
            _logger = logger;

            // Initialize stock prices
            _stockPrices = new Dictionary<string, decimal>
            {
                { "AAPL", 150m },
                { "TSLA", 300m },
                { "NVDA", 450m }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Iterate through all tickers
                foreach (var ticker in _stockPrices.Keys)
                {
                    // Generate a new price for the current ticker
                    var newPrice = GenerateNewPrice(ticker);

                    // Call the PricePublisher to publish the price update
                    await _pricePublisher.PublishPriceUpdateAsync(ticker, newPrice);

                    Console.WriteLine($"Published price for {ticker}: {newPrice}");
                    _logger.LogInformation("Price updated for ticker: {ticker}", ticker);
                }
                
                // Wait for X seconds before generating and publishing the next round of prices
                await Task.Delay(5000, stoppingToken);
            }            
        }

        // Method to generate a new price for a given stock ticker
        private decimal GenerateNewPrice(string ticker)
        {
            // Generate a random price fluctuation between -5% and +5%
            var percentageChange = (decimal)(_random.NextDouble() * 0.1 - 0.05);
            var newPrice = _stockPrices[ticker] * (1 + percentageChange);

            // Update the internal price for the ticker
            _stockPrices[ticker] = Math.Round(newPrice, 2);

            return _stockPrices[ticker];
        }
    }
}
