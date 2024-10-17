using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PortfolioService
{
    public class Portfolio
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public decimal TotalValue { get; set; }
        
        //public Dictionary<string, decimal> StockHoldings { get; set; } = new Dictionary<string, decimal>(); // Stock Ticker and Quantity
        
        //One-to-many relationship with StockHolding
        public ICollection<StockHolding> StockHoldings { get; set; }
    }

    public class StockHolding
    {
        public int Id { get; set; } // Primary key
        public required string Ticker { get; set; } // Stock ticker (e.g., AAPL, TSLA)
        public int Quantity { get; set; } // Number of stocks held
        //public decimal Price { get; set; } // Latest price of the stock

        // Foreign key to the portfolio
        public int PortfolioId { get; set; }
        public Portfolio Portfolio { get; set; } // Navigation property
    }
}
