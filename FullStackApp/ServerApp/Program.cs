var builder = WebApplication.CreateBuilder(args);

// Configure CORS to allow requests from Blazor client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient",
        policy => policy
            .WithOrigins("http://localhost:<CLIENT_APP_PORT>") // Replace <CLIENT_APP_PORT> with the actual port numbers
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Configure output caching - must vary by Origin header for proper CORS support
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByHeader("Origin"));
});

var app = builder.Build();

// IMPORTANT: UseCors must come before UseOutputCache to ensure cached responses include CORS headers
app.UseCors("AllowBlazorClient");
app.UseOutputCache();

// API endpoint returning product catalog with nested category and supplier data
app.MapGet("/api/productlist", () =>
{
    return Results.Json(new[]
    {
        new
        {
            Id = 1,
            Name = "Laptop",
            Price = 1200.50,
            Stock = 25,
            Available = true,
            Category = new { Id = 101, Name = "Electronics" },
            Supplier = new { Name = "TechCorp", Location = "USA" }
        },
        new
        {
            Id = 2,
            Name = "Headphones",
            Price = 50.00,
            Stock = 100,
            Available = true,
            Category = new { Id = 102, Name = "Accessories" },
            Supplier = new { Name = "AudioPlus", Location = "Japan" }
        }
    },
    new System.Text.Json.JsonSerializerOptions
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    });
})
.CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));

app.Run();