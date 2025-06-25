# Redis with cache update
This lab is based on this article - https://codewithmukesh.com/blog/distributed-caching-in-aspnet-core-with-redis/

Also you might want take a look to the interface with which we will work in this lab - `IDistributedCache`. Microsoft docs - https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-9.0

## Description
Before we start, I want to pay your attention to the idea of this current branch. Idea is to show 
my experiments with performance that CQRS approach can give. So I won't describe how to build up the code
from the beginning. 

Current branch is based on `store-in-json-format`(this branch is the same repo, you can find it in the branch list). In `store-in-json-format` you will find
how to write code that implements CQRS approaches and save cache into `Redis` in JSON format. 

Here is the link to branch - https://github.com/BekAllaev/RedisWithCacheUpdate/tree/store-in-json-format

## Instructions
So as I have mentioned above we won't talk about how to write code from the scratch. Here I will just tell you, what changes I have made with regard to 
`store-in-json-format`.

1. First of all I removed `Sqlite` database.   
I used `MS SQL Server`, the reason I used this one is because I wanted to be a little bit closer
to real-world dev situations. So feel free to remove every NuGet package and code that is related to `Sqlite` and download `Microsoft.EntityFrameworkCore.SqlServer` NuGet package
2. Register your `DbContext` for Sql server:
```
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var connectionString = builder.Configuration.GetConnectionString(SqlServerDbConnectionString);
    opt.UseSqlServer(connectionString);
});
```

> `SqlServerDbConnectionString` is constant with the name of connection string to MS SQL Server database in `appsettings.json`.

3. Before every run I delete databasae and create it again with this code:
```
await context.Database.EnsureDeletedAsync();
context.Database.Migrate();
```

4. The seeder is little bit different:
```
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
```
> The method seed about 1 million products and 200 categories

> Call it right after the migration. After `context.Database.Migrate()`

5. Then you need to generate migration for MS SQL Server, don't run it. Migration will be run when you start the app.

6. In `StatisticsController` add this two endpoint:
```
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
```

You can see that these methods creates statistics on-demand. On the opposite the method that works with Redis just reads ready statistics.

The first method make request against `Product` table, so it first groups products and then count the number of products in each group.  
The second method make request against `Category` table, so it directly goes through every category and counts number of products.

I create them so you can see how different approaches can lead to one result but still one of them will be faster than another one.

## Results

Take a note please, that this WebAPI is build on Net 9, so you won't have Swagger. So you will need to download OpenApi document that describes REST API. 
For current backend you can download it from this url - `https://localhost:7103/openapi/v1.json`, navigate to it when you have run the app.

Save content somewhere and then import it into the Postman.

So, let's start with checking "Redis" endpoint. Here is the result:
![image](https://github.com/user-attachments/assets/1febf903-4e16-488f-a09c-7d07f1aa9461)

Then "On demand with grouping". Here is the result:
![image](https://github.com/user-attachments/assets/d340e0b7-915f-484e-8c6e-1b7c9da9d8b5)

And "On demand without grouping". Here is the result:
![image](https://github.com/user-attachments/assets/d32e5983-8492-40f2-9ef5-37cc270204cf)

## Conclusion
So you can see that reading data from read stack, there are stored calculated statistics, is more then 20 time faster, plus we store them in Redis
