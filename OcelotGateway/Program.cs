using Common.Extensions;
using Common.Middlewares;
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

//app.UseHttpsRedirection();
app.UseCors();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.UseSerilogRequestLogging();
await app.UseOcelot();
//app.UseMiddleware<CustomAuthenticationMiddleware>();

app.Run();


//Nas configurações do Ocelot, foi assumida a porta padrão para https, encontrar forma de não passar e não assumir