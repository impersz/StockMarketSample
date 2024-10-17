namespace OrderService.Models
{
    public class Order
    {
        public int Id { get; set; }
        public required string Ticker { get; set; }
        public int Quantity { get; set; }
        public required string Side { get; set; } // buy or sell
        public decimal Price { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
