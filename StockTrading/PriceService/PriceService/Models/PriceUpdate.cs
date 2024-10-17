using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceService
{
    public class PriceUpdate
    {       
        public DateTime Timestamp { get; set; } // Timestamp of the price update
   
        public required string Ticker { get; set; } // Stock ticker (e.g., AAPL)

        public decimal Price { get; set; } // New stock price        
    }
}
