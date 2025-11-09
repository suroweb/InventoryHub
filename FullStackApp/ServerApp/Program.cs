using System.Threading.RateLimiting;
using InventoryHub.Shared.Models;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/inventoryhub-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting InventoryHub API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog for all logging
    builder.Host.UseSerilog();

    // Get configuration values
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5173" };
    var cacheDuration = builder.Configuration.GetValue<int>("Caching:OutputCacheDurationMinutes", 5);
    var rateLimitPermits = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
    var rateLimitWindow = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);

    // Configure CORS with configuration-based origins
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorClient",
            policy => policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    // Configure output caching - must vary by Origin header for proper CORS support
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(builder => builder
            .Expire(TimeSpan.FromMinutes(cacheDuration))
            .SetVaryByHeader("Origin"));
    });

    // Configure rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitPermits,
                    Window = TimeSpan.FromSeconds(rateLimitWindow),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
        };
    });

    // Add health checks
    builder.Services.AddHealthChecks();

    // Add OpenAPI/Swagger for API documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline

    // Global exception handler middleware
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            if (exception != null)
            {
                Log.Error(exception, "Unhandled exception occurred");
            }

            await context.Response.WriteAsJsonAsync(new
            {
                error = "An error occurred processing your request.",
                traceId = context.TraceIdentifier
            });
        });
    });

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        await next();
    });

    // Enable HTTPS redirection in production
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // IMPORTANT: UseCors must come before UseOutputCache to ensure cached responses include CORS headers
    app.UseCors("AllowBlazorClient");
    app.UseRateLimiter();
    app.UseOutputCache();

    // Health check endpoints
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapHealthChecks("/health/live").AllowAnonymous();

    // API endpoint returning product catalog with nested category and supplier data
    app.MapGet("/api/productlist", () =>
    {
        Log.Information("Fetching product list");

        var products = new List<Product>
        {
            new()
            {
                Id = 1,
                Name = "Laptop",
                Price = 1200.50,
                Stock = 25,
                Available = true,
                Category = new Category { Id = 101, Name = "Electronics" },
                Supplier = new Supplier { Name = "TechCorp", Location = "USA" }
            },
            new()
            {
                Id = 2,
                Name = "Headphones",
                Price = 50.00,
                Stock = 100,
                Available = true,
                Category = new Category { Id = 102, Name = "Accessories" },
                Supplier = new Supplier { Name = "AudioPlus", Location = "Japan" }
            },
            new()
            {
                Id = 3,
                Name = "Wireless Mouse",
                Price = 25.99,
                Stock = 150,
                Available = true,
                Category = new Category { Id = 102, Name = "Accessories" },
                Supplier = new Supplier { Name = "TechCorp", Location = "USA" }
            },
            new()
            {
                Id = 4,
                Name = "4K Monitor",
                Price = 399.99,
                Stock = 45,
                Available = true,
                Category = new Category { Id = 101, Name = "Electronics" },
                Supplier = new Supplier { Name = "DisplayTech", Location = "South Korea" }
            },
            new()
            {
                Id = 5,
                Name = "Mechanical Keyboard",
                Price = 89.99,
                Stock = 0,
                Available = false,
                Category = new Category { Id = 102, Name = "Accessories" },
                Supplier = new Supplier { Name = "KeyMasters", Location = "Germany" }
            }
        };

        return Results.Json(products, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
    })
    .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(cacheDuration)))
    .WithName("GetProducts")
    .WithDescription("Retrieves the complete product catalog with categories and suppliers")
    .WithTags("Products")
    .Produces<List<Product>>(StatusCodes.Status200OK);

    // API versioning endpoint
    app.MapGet("/api/version", () => Results.Json(new
    {
        version = "1.0.0",
        environment = app.Environment.EnvironmentName,
        timestamp = DateTime.UtcNow
    }))
    .WithName("GetVersion")
    .WithDescription("Returns API version and environment information")
    .WithTags("System");

    // Enable OpenAPI in development
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    Log.Information("InventoryHub API started successfully on {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to tests
public partial class Program { }
