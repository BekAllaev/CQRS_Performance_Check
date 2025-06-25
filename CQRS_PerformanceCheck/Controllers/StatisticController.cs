using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RedisWithCacheUpdate.Data;
using RedisWithCacheUpdate.Extensions;
using RedisWithCacheUpdate.Services;
using RedisWithCacheUpdate.StatisticsModel;
using StackExchange.Redis;

namespace RedisWithCacheUpdate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticController : ControllerBase
    {
        private readonly IProductsByCateogryCacheService productsByCateogryCacheService;
        private readonly AppDbContext _dbContext;

        public StatisticController(IProductsByCateogryCacheService productsByCateogryCacheService, AppDbContext dbContext)
        {
            this.productsByCateogryCacheService = productsByCateogryCacheService;
            _dbContext = dbContext;
        }

        [HttpGet("redis")]
        public async Task<ActionResult<IEnumerable<ProductsByCategory>>> GetProductsByCategoriesFromRedis()
        {
            var list = (await productsByCateogryCacheService.GetListFromCacheAsync()).ToList();

            return list;
        }

        [HttpGet("onDemandWithGrouping")]
        public async Task<ActionResult<IEnumerable<ProductsByCategory>>> GetProductsByCategoriesWithGrouping()
        {
            var list = await _dbContext.Products.GroupBy(x => x.CategoryId).Select(x => new ProductsByCategory()
            {
                CategoryName = _dbContext.Categories.Single(y => y.Id == x.Key).Name,
                ProductCount = x.Count()
            }).ToListAsync();

            return list;
        }

        [HttpGet("onDemandWithoutGrouping")]
        public async Task<ActionResult<IEnumerable<ProductsByCategory>>> GetProductsByCategoriesWithoutGrouping()
        {
            var list = await _dbContext.Categories.Select(x => new ProductsByCategory() { CategoryName = x.Name, ProductCount = x.Products.Count }).ToListAsync();

            return list;
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<ProductsByCategory>> GetProductsByCategory(string key)
        {
            var productByCategory = await productsByCateogryCacheService.GetByKeyAsync(key);

            return productByCategory;
        }

    }
}
