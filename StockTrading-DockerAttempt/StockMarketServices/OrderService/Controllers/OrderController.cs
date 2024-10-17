using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderService.Data;
using OrderService.Messages;
using OrderService.Models;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IBus _bus;
        private readonly ILogger<OrderController> _logger;

        public OrderController(OrderDbContext context, IBus bus, ILogger<OrderController> logger)
        {
            _context = context;
            _bus = bus;
            _logger = logger;
        }

        // POST: api/order/add/{userId}
        [HttpPost("add/{userId}")]
        public async Task<IActionResult> AddOrder(int userId, [FromBody] Order order)
        {
            if (order == null || string.IsNullOrEmpty(order.Ticker) || order.Quantity <= 0 || string.IsNullOrEmpty(order.Side))
                return BadRequest("Invalid order details");
            //TODO: Temporary, use fluentValidation or something else later.

            order.UserId = userId;
            order.CreatedAt = DateTime.UtcNow;
  
            order.Price = 100.0M; // Placeholder for now

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Order created: {order.Ticker}, {order.Quantity} at {order.Price} for user {userId}");

            // Publish an event to notify PortfolioService
            await _bus.Publish(new OrderPlacedEvent
            {
                OrderId = order.Id,
                UserId = userId,
                Ticker = order.Ticker,
                Quantity = order.Quantity,
                Price = order.Price,
                CreatedAt = order.CreatedAt
            });

            _logger.LogInformation($"OrderPlacedEvent published for OrderId {order.Id}");

            return CreatedAtAction(nameof(AddOrder), new { id = order.Id }, order);

        }
    }
}
