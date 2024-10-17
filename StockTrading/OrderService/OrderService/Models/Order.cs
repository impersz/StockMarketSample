namespace OrderService.Models
{
    namespace OrderService.Models
    {
        public class Order
        {
            public int Id { get; set; }
            public required string UserId { get; set; }
            public required string Ticker { get; set; }
            public int Quantity { get; set; }
            public required string Side { get; set; } // buy or sell
            public decimal Price { get; set; }
            public DateTime OrderDate { get; set; }
        }

        public class OrderDto
        {
            public required string Ticker { get; set; }
            public int Quantity { get; set; }
            public required string Side { get; set; } // buy or sell
        }
    }
}
