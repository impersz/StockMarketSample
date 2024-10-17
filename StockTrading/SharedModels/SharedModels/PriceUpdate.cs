namespace SharedModels
{
    public class PriceUpdate
    {
        public DateTime Timestamp { get; set; } // Timestamp of the price update

        public required string Ticker { get; set; } // Stock ticker (e.g., AAPL)

        public decimal Price { get; set; } // New stock price        
    }
}
