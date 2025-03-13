using System.Threading.RateLimiting;
using Common.Extensions;
using Common.Middlewares;
using Delfos.Authentication.AzureAdAdmin;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.UseCustomOpenTelemetry("YARP");
builder.Services.AddSingleton(typeof(ITokenService), typeof(TokenService));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("fixedCode", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress + ctx.Request.Path.Value,
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        })
    );
});
builder.Services.AddAuthenticationAzureAdAdmin(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AzureAdAdmin", policy => policy.RequireAuthenticatedUser());
});
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((_, handler) => handler.ActivityHeadersPropagator = null); // Disable Activity propagation, may update headers

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});
var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use((context, next) =>
    {
        // Custom inline middleware
        var proxyFeature = context.GetReverseProxyFeature();
        var cluster = proxyFeature.Cluster;
        var destinations = proxyFeature.AvailableDestinations;

        return next();
    });
    //proxyPipeline.UseMiddleware<CustomAuthenticationMiddleware>();
    proxyPipeline.UseMiddleware<CustomHeadersMiddleware>();
});
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseSerilogRequestLogging();

app.Run();