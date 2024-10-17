using MassTransit;
using Microsoft.AspNetCore.Mvc;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Models;
using OrderService.Models.OrderService.Models;
using OrderService.Services;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly PriceCache _priceCache;

        private readonly ILogger<OrderController> _logger;


        public OrderController(OrderDbContext context, PriceCache priceCache, 
            ILogger<OrderController> logger)
        {
            _context = context;
            _priceCache = priceCache;
            _logger = logger;
        }

        // POST: api/order/add/{userId}
        [HttpPost("add/{userId}")]
        public async Task<IActionResult> PlaceOrder(string userId, [FromBody] OrderDto orderDto)
        {
            // Get the latest price from the cache
            var price = _priceCache.GetPrice(orderDto.Ticker);

            if (price == 0)
            {
                return BadRequest("Unable to retrieve the latest price.");
            }

            //TODO: Logic for checking if user has enough stocks when he is trying to sell.

            var order = new Order 
            { 
                UserId = userId,
                Ticker = orderDto.Ticker,
                Side = orderDto.Side,
                Quantity = orderDto.Quantity
            };

            //order.UserId = userId;
            order.Price = price;
            order.OrderDate = DateTime.UtcNow;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Log the order (optional for debugging)
            Console.WriteLine($"Db Saved order: {userId} - {order.Ticker} - {order.Quantity} - {order.Price}");

            // Create an OrderPlacedEvent
            var orderPlacedEvent = new OrderPlacedEvent
            {
                UserId = order.UserId,
                Ticker = order.Ticker,
                Quantity = order.Quantity,
                Side = order.Side,
                Price = order.Price,
                OrderDate = order.OrderDate
            };

            // Publish the OrderPlacedEvent to RabbitMQ
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "order-placed-queue",
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

            // Serialize object to JSON string
            string jsonString = JsonSerializer.Serialize(orderPlacedEvent);
            // Convert JSON string to byte array
            byte[] body = Encoding.UTF8.GetBytes(jsonString);

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: "order-placed-queue",
                                 basicProperties: null,
                                 body: body);

            Console.WriteLine($"Published orderPlacedEvent: {userId} - {order.Ticker} - {order.Quantity} - {order.Price}");

            return Ok(order);
        }
    }
}