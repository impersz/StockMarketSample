using MassTransit;
using Microsoft.Extensions.Hosting;
using PriceService.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PriceService.Services
{
    public class PriceGeneratorService : BackgroundService
    {
        private readonly IBus _bus;
        private readonly Random _random = new Random();
        private readonly List<string> _tickers = new List<string> { "AAPL", "TSLA", "NVDA", "MSFT" };

        public PriceGeneratorService(IBus bus)
        {
            _bus = bus;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var ticker in _tickers)
                {
                    var price = new Price
                    {
                        Ticker = ticker,
                        CurrentPrice = _random.Next(100, 600), // Random price between 100 and 600 based on appropriate price ranges as of 2024. Revisit later.
                        Timestamp = DateTime.UtcNow
                    };

                    // Publish price to RabbitMQ
                    await _bus.Publish(price);

                    Console.WriteLine($"Published price for {ticker}: {price.CurrentPrice}");
                }

                await Task.Delay(10000, stoppingToken); // Wait for 1 second before generating new prices. // Temporary 10seconds
            }
        }
    }
}
