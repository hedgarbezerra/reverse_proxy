using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Common.Policies
{
    public class DefaultRateLimitingPolicy : IRateLimiterPolicy<string>
    {
        public const string Name = "default-endpoint-policy";
        public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return new ValueTask();
        };

        public RateLimitPartition<string> GetPartition(HttpContext httpContext) => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress + httpContext.Request.Path.Value,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
    }
}
