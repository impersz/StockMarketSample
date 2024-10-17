namespace PriceService.Models
{
    public class Price
    {
        public required string Ticker { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
