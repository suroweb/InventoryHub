using Microsoft.Extensions.Caching.Memory;
using ServerApp.Services;

namespace ServerApp.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ISubscriptionService subscriptionService)
    {
        // Skip rate limiting for auth endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        try
        {
            var tenantId = tenantService.GetTenantId();
            var cacheKey = $"rate_limit_{tenantId}_{DateTime.UtcNow:yyyyMMddHHmm}";

            var usage = await subscriptionService.GetTenantUsageAsync(tenantId);
            var limit = usage.ApiRateLimit;

            var currentCount = _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return 0;
            });

            if (currentCount >= limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = $"API rate limit of {limit} requests per minute exceeded",
                    retryAfter = 60 - DateTime.UtcNow.Second
                });
                return;
            }

            _cache.Set(cacheKey, currentCount + 1, TimeSpan.FromMinutes(1));

            // Add rate limit headers
            context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
            context.Response.Headers.Append("X-RateLimit-Remaining", (limit - currentCount - 1).ToString());
            context.Response.Headers.Append("X-RateLimit-Reset", DateTime.UtcNow.AddMinutes(1).ToString("o"));

            await _next(context);
        }
        catch (InvalidOperationException)
        {
            // Tenant context not set, let the tenant middleware handle it
            await _next(context);
        }
    }
}

public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
