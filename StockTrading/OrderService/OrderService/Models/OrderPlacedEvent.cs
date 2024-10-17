namespace OrderService.Models
{
    public class OrderPlacedEvent
    {
        public required string UserId { get; set; }
        public required string Ticker { get; set; }
        public int Quantity { get; set; }
        public required string Side { get; set; }
        public decimal Price { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
