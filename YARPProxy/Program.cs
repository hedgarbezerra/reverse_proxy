using Common.Extensions;
using Common.Middlewares;
using Common.Policies;
using Microsoft.OpenApi.Models;
using Serilog;
using Yarp.ReverseProxy;
using YARPProxy.Services;

var builder = WebApplication.CreateBuilder(args);

builder.UseCustomOpenTelemetry("YARP");

builder.Services.AddSingleton<ISwaggerEndpointManager, SwaggerEndpointManager>();
builder.Services.AddSingleton(typeof(ITokenService), typeof(TokenService));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy<string,DefaultRateLimitingPolicy>(DefaultRateLimitingPolicy.Name);
});
builder.Services.AddSecurityServices();
builder.Services.AddHttpClient();  
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsDefaultPolicy.Name, CorsDefaultPolicy.CorsPolicy);
});


var app = builder.Build();

app.UseCors(CorsDefaultPolicy.Name);
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
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var stateLookup = app.Services.GetRequiredService<IProxyStateLookup>();
    foreach (var route in stateLookup.GetRoutes().Where(r => !r.Config.RouteId.Contains("poke")))
    {
        string title = $"{route.Config.RouteId} - v1";
        var (key, endpoint) = route.Cluster.Destinations.First();
        options.SwaggerEndpoint(
            $"{endpoint.Model.Config.Address}/swagger/v1/swagger.json", title);
        options.EnableDeepLinking();
        options.DisplayOperationId();
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
    }
});

await app.RunAsync();



// builder.Services.AddSwaggerGen(options =>
// {
//     var stateLookup = builder.Services.BuildServiceProvider()
//         .GetRequiredService<IProxyStateLookup>();
//
//     foreach (var route in stateLookup.GetRoutes())
//     {
//         options.SwaggerDoc(
//             description.GroupName,
//             new OpenApiInfo
//             {
//                 Title = $"WebAPI {description.ApiVersion}",
//                 Version = description.ApiVersion.ToString(),
//                 Description = description.IsDeprecated ? "Esta versão está depreciada." : string.Empty
//             });
//     }
//     {
//         options.SwaggerDoc(
//             description.GroupName,
//             new OpenApiInfo
//             {
//                 Title = $"WebAPI {description.ApiVersion}",
//                 Version = description.ApiVersion.ToString(),
//                 Description = description.IsDeprecated ? "Esta versão está depreciada." : string.Empty
//             });
//     }
// });

//TODO: Pode ser necessário adicionar esse trecho quando acessar uma API externa e é importante o cors:
//app.UseSwagger(c =>
// {
//     c.PreSerializeFilters.Add((swagger, httpReq) =>
//     {
//         swagger.Servers = new List<OpenApiServer>
//         {
//             new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
//         };
//     });
// });