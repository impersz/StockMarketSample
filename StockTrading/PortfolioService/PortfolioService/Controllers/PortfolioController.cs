using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;

namespace PortfolioService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly PortfolioDbContext _dbContext;

        public PortfolioController(PortfolioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetPortfolio(string userId)
        {
            var portfolio = await _dbContext.Portfolios
                                            //.Include(p => p.StockHoldings)
                                            .FirstOrDefaultAsync(p => p.UserId == userId);
            if (portfolio == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                portfolio.UserId,
                portfolio.TotalValue
            });
        }
    }

}
