using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioService.Data;
using PortfolioService.Services;

namespace PortfolioService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly PortfolioDbContext _dbContext;
        private readonly PriceCache _priceCache;

        public PortfolioController(PortfolioDbContext dbContext, PriceCache priceCache)
        {
            _dbContext = dbContext;
            _priceCache = priceCache;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetPortfolio(string userId)
        {
            var portfolio = await _dbContext.Portfolios
                                            .Include(p => p.StockHoldings)
                                            .FirstOrDefaultAsync(p => p.UserId == userId);
            if (portfolio == null)
            {
                return NotFound();
            }

            decimal totalValue = 0;

            foreach(var sh in portfolio.StockHoldings)
            {
                totalValue += sh.Quantity * _priceCache.GetPrice(sh.Ticker);
            }

            return Ok(new
            {
                portfolio.UserId,
                totalValue
            });
        }
    }

}
