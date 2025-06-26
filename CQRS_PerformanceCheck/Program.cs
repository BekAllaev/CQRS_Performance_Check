using Bogus;
using Microsoft.EntityFrameworkCore;
using RedisWithCacheUpdate.Data;
using RedisWithCacheUpdate.Model;
using RedisWithCacheUpdate.Services;
using System.Threading.Tasks;

namespace RedisWithCacheUpdate
{
    public class Program
    {
        private const string SqliteDbConnectionString = "Sqlite";

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddDbContext<AppDbContext>(opt =>
            {
                var connectionString = builder.Configuration.GetConnectionString(SqliteDbConnectionString);
                opt.UseSqlite(connectionString);
            });

            builder.Services.AddScoped<IProductsByCateogryCacheService, ProductsByCategoryCacheService>();

            var app = builder.Build();

            var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            context.Database.Migrate();

            if (TablesAreEmpty(context))
            {
                await SeedData(context);

                var productsByCategoryCacheService = scope.ServiceProvider.GetRequiredService<IProductsByCateogryCacheService>();
    
                await productsByCategoryCacheService.SetCacheAsync();
            }

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

        private static bool TablesAreEmpty(AppDbContext context)
        {
            return !context.Categories.Any() && !context.Products.Any() && !context.ProductsByCategories.Any();
        }

        static async Task SeedData(AppDbContext context)
        {
            // Create a Faker for the Category model.
            var categoryFaker = new Faker<Category>()
                .RuleFor(c => c.Name, f => $"Category - {f.UniqueIndex}");

            // Create a Faker for the Product model.
            // Notice that the CategoryId will be assigned later for each product.
            var productFaker = new Faker<Product>()
                .RuleFor(p => p.Name, f => $"Product - {f.UniqueIndex}");

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
