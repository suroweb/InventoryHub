using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using ServerApp.Data.Contexts;
using ServerApp.Data.Repositories;
using ServerApp.Domain.Entities;
using ServerApp.Endpoints;
using ServerApp.Middleware;
using ServerApp.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/inventoryhub-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InventoryHub API",
        Version = "v1",
        Description = "Multi-tenant SaaS Inventory Management API"
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Master Database (for tenants and authentication)
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("MasterDatabase") ??
        "Host=localhost;Database=InventoryHub_Master;Username=postgres;Password=postgres"));

// Configure Tenant Database (will be dynamically configured per tenant)
builder.Services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
{
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var tenantService = serviceProvider.GetRequiredService<ITenantService>();

    try
    {
        var connectionString = tenantService.GetTenantConnectionStringAsync().GetAwaiter().GetResult();
        options.UseNpgsql(connectionString);
    }
    catch
    {
        // Default connection string for initial setup
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultTenant") ??
            "Host=localhost;Database=InventoryHub_Tenant;Username=postgres;Password=postgres");
    }
});

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<MasterDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "InventoryHub";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "InventoryHub";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:5001",
                "https://localhost:5000",
                "https://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add Output Caching
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByHeader("Origin", "Authorization"));
});

// Add Memory Cache for rate limiting
builder.Services.AddMemoryCache();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register application services
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "InventoryHub API v1");
    });
}

app.UseHttpsRedirection();

// IMPORTANT: Middleware order matters!
app.UseCors("AllowBlazorClient");

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware
app.UseTenantResolution();
app.UseRateLimiting();

app.UseOutputCache();

// Map endpoints
app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapProductEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health")
.WithOpenApi();

// API Info endpoint
app.MapGet("/api", () => Results.Ok(new
{
    name = "InventoryHub API",
    version = "1.0.0",
    description = "Multi-tenant SaaS Inventory Management System",
    features = new[]
    {
        "Multi-tenant architecture with database-per-tenant isolation",
        "JWT authentication and authorization",
        "Subscription tier management",
        "API rate limiting per tenant",
        "RESTful API with OpenAPI documentation"
    },
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
        auth = "/api/auth",
        tenants = "/api/tenants",
        products = "/api/v1/products"
    }
}))
.WithName("ApiInfo")
.WithTags("Info")
.WithOpenApi()
.AllowAnonymous();

Log.Information("🚀 InventoryHub API starting...");
Log.Information("📊 Swagger UI available at /swagger");
Log.Information("🏥 Health check available at /health");

app.Run();
