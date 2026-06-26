using System.Security.Claims;
using System.Threading.RateLimiting;
using EShop.ApiGateway.Options;
using Microsoft.AspNetCore.RateLimiting;

namespace EShop.ApiGateway.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection("RateLimiting")
            .Get<RateLimitingOptions>()
            ?? RateLimitingOptions.Default();

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.AddPolicy<string>("auth", CreateAuthPolicy(options.Auth));
            limiterOptions.AddPolicy<string>("general", CreateGeneralPolicy(options.General));
        });

        return services;
    }

    private static Func<HttpContext, RateLimitPartition<string>> CreateAuthPolicy(
        RateLimitingOptions.PolicyOptions options)
    {
        return httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromMinutes(options.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
    }

    private static Func<HttpContext, RateLimitPartition<string>> CreateGeneralPolicy(
        RateLimitingOptions.PolicyOptions options)
    {
        return httpContext =>
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId is not null)
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"user:{userId}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimitAuthenticated ?? 100,
                        Window = TimeSpan.FromMinutes(options.WindowMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            }

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimitAnonymous ?? 30,
                    Window = TimeSpan.FromMinutes(options.WindowMinutes),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        };
    }
}
