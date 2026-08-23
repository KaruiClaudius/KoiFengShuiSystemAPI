using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace KoiFengShuiSystem.Shared.Kernel.Modules
{
    public static class RateLimitingExtensions
    {
        private const string GlobalPermitPerMinuteKey = "RateLimiting:GlobalPermitPerMinute";
        private const string AuthPermitPerMinuteKey = "RateLimiting:AuthPermitPerMinute";
        private const string ComputePermitPerMinuteKey = "RateLimiting:ComputePermitPerMinute";

        public const string AuthPolicyName = "auth";
        public const string ComputePolicyName = "compute";

        public static IServiceCollection AddConfiguredRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                var globalPermitPerMinute = ReadPermitPerMinute(configuration, GlobalPermitPerMinuteKey, fallbackPermitsPerMinute: 120);
                var authPermitPerMinute = ReadPermitPerMinute(configuration, AuthPermitPerMinuteKey, fallbackPermitsPerMinute: 10);
                var computePermitPerMinute = ReadPermitPerMinute(configuration, ComputePermitPerMinuteKey, fallbackPermitsPerMinute: 30);

                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(httpContext),
                        _ => CreateFixedWindowOptions(globalPermitPerMinute)));

                options.AddPolicy(AuthPolicyName, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(httpContext),
                        _ => CreateFixedWindowOptions(authPermitPerMinute)));

                options.AddPolicy(ComputePolicyName, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ResolvePartitionKey(httpContext),
                        _ => CreateFixedWindowOptions(computePermitPerMinute)));
            });

            return services;
        }

        public static IApplicationBuilder UseConfiguredRateLimiter(this IApplicationBuilder app) =>
            app.UseRateLimiter();

        private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(int permitLimit) => new()
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        };

        private static string ResolvePartitionKey(HttpContext httpContext) =>
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private static int ReadPermitPerMinute(IConfiguration configuration, string key, int fallbackPermitsPerMinute) =>
            Math.Max(1, configuration.GetValue(key, fallbackPermitsPerMinute));
    }
}
