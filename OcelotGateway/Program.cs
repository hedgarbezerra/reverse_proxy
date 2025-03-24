using Common.Extensions;
using Common.Middlewares;
using Common.Policies;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.UseCustomOpenTelemetry("Ocelot");
builder.Services.AddSingleton(typeof(ITokenService), typeof(TokenService));

var hostingContext = builder.Services.BuildServiceProvider().GetRequiredService<IHostEnvironment>();
var configurationFileName = hostingContext.EnvironmentName switch
{
    "Testing" => "ocelot-settings.Testing.json",
    "Development" => "ocelot-settings.Development.json",
    _ => "ocelot-settings.json",
};
builder.Configuration.AddJsonFile(configurationFileName, true, true);
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy<string, DefaultRateLimitingPolicy>(DefaultRateLimitingPolicy.Name);
});
builder.Services.AddSecurityServices();
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
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseSerilogRequestLogging();
await app.UseOcelot();

await app.RunAsync();