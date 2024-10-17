namespace OrderService.Messages
{
    public class OrderPlacedEvent
    {
        public int OrderId { get; set; }
        public int UserId { get; set; } //Already started it as Int instead of String, will keep it like that for now. Revisit later.
        public required string Ticker { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
