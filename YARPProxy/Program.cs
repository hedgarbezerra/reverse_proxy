using Common.Extensions;
using Common.Middlewares;
using Common.Policies;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


builder.UseCustomOpenTelemetry("YARP");
builder.Services.AddSingleton(typeof(ITokenService), typeof(TokenService));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy<string,DefaultRateLimitingPolicy>(DefaultRateLimitingPolicy.Name);
});

builder.Services.AddSecurityServices();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsDefaultPolicy.Name, CorsDefaultPolicy.CorsPolicy);
});


var app = builder.Build();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use((context, next) =>
    {
        var proxyFeature = context.GetReverseProxyFeature();
        var cluster = proxyFeature.Cluster;
        var destinations = proxyFeature.AvailableDestinations;

        return next();
    });
    proxyPipeline.UseMiddleware<CustomHeadersMiddleware>();
});
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseSerilogRequestLogging();

await app.RunAsync();