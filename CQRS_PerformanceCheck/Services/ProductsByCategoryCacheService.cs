using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RedisWithCacheUpdate.Data;
using RedisWithCacheUpdate.Model;
using RedisWithCacheUpdate.StatisticsModel;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace RedisWithCacheUpdate.Services
{
    public class ProductsByCategoryCacheService(AppDbContext context) : IProductsByCateogryCacheService
    {
        public async Task<ProductsByCategory?> GetByKeyAsync(string key)
        {
            return await context.ProductsByCategories.SingleOrDefaultAsync(x => x.CategoryName == key);
        }

        public async Task<List<ProductsByCategory>> GetListFromCacheAsync()
        {
            return await context.ProductsByCategories.ToListAsync();
        }

        public async Task SetCacheAsync()
        {
            var statistics = await GetStatistics();

            await context.ProductsByCategories.AddRangeAsync(statistics);

            await context.SaveChangesAsync();
        }

        public Task UpdateCacheAsync()
        {
            throw new NotImplementedException();
        }

        private Task<List<ProductsByCategory>> GetStatistics()
        {
            var stastics = context
                .Categories
                .Select(x => new ProductsByCategory
                {
                    CategoryName = x.Name,
                    ProductCount = x.Products.Count()
                })
                .ToListAsync();

            return stastics;
        }
    }
}
