using Bogus;
using Microsoft.EntityFrameworkCore;
using RedisWithCacheUpdate.Data;
using RedisWithCacheUpdate.Model;
using RedisWithCacheUpdate.Services;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace RedisWithCacheUpdate
{
    public class Program
    {
        private const string RedisConnectionStringName = "Redis";
        private const string SqlServerDbConnectionString = "SqlServer";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var redisConnectionString = builder.Configuration.GetConnectionString(RedisConnectionStringName);

            builder.Services.AddDbContext<AppDbContext>(opt =>
            {
                var connectionString = builder.Configuration.GetConnectionString(SqlServerDbConnectionString);
                opt.UseSqlServer(connectionString);
            });

            if (string.IsNullOrEmpty(redisConnectionString))
                throw new ArgumentNullException(nameof(redisConnectionString));

            var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);

            builder.Services.AddSingleton<ConnectionMultiplexer>(connectionMultiplexer);

            builder.Services.AddScoped<IProductsByCateogryCacheService, ProductsByCategoryCacheService>();

            var app = builder.Build();

            var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await context.Database.EnsureDeletedAsync();
            context.Database.Migrate();

            await SqlServerSeedData(context);

            var productsByCategoryCacheService = scope.ServiceProvider.GetRequiredService<IProductsByCateogryCacheService>();

            await productsByCategoryCacheService.SetCacheAsync();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        static async Task SqlServerSeedData(AppDbContext context)
        {
            // Create a Faker for the Category model.
            var categoryFaker = new Faker<Category>()
                .RuleFor(c => c.Name, f => f.Commerce.Department())
                .RuleFor(c => c.Description, f => f.Lorem.Sentence());

            // Create a Faker for the Product model.
            // Notice that the CategoryId will be assigned later for each product.
            var productFaker = new Faker<Product>()
                .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                .RuleFor(p => p.UnitPrice, f => Convert.ToDouble(f.Commerce.Price()));

            // Generate 5 categories.
            var categories = categoryFaker.Generate(200);

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var allProducts = new List<Product>();

            var categoriesFromDb = await context.Categories.ToListAsync();

            // For each category, generate between 1 to 10 products.
            foreach (var category in categoriesFromDb)
            {
                int productCount = new Faker().Random.Int(1, 10000);
                // Clone the productFaker to assign CategoryId specifically for this category.
                var productsForCategory = productFaker
                    .Clone()
                    .RuleFor(p => p.CategoryId, f => category.Id)
                    .Generate(productCount);

                allProducts.AddRange(productsForCategory);
            }

            // Add to the context and save changes.
            context.Products.AddRange(allProducts);
            await context.SaveChangesAsync();
        }
    }
}
