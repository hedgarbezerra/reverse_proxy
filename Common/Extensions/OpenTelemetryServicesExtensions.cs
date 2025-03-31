using System.Diagnostics;
using System.Text.Json;
using Common.DTOs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Exceptions;
using Serilog.Sinks.Grafana.Loki;

namespace Common.Extensions;

public static class OpenTelemetryExtensions
{
    public static async Task<RequestData> GetData(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var headers = request.Headers;
        var jsonHeaders = JsonSerializer.Serialize(headers);

        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        var requestData = new RequestData(jsonHeaders, body);
        return requestData;
    }

    public static WebApplicationBuilder UseSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .WriteTo.Console(theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)
            .WriteTo.Debug()
            .WriteTo.GrafanaLoki("http://loki:3100",
                useInternalTimestamp: true,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                labels: [NewLabel("service_name", serviceName)])
            .CreateLogger();

        builder.Services.AddSerilog();
        builder.Host.UseSerilog();

        return builder;

        LokiLabel NewLabel(string key, string value) => new LokiLabel { Key = key, Value = value };
    }
    
    public static WebApplicationBuilder UseCustomOpenTelemetry(this WebApplicationBuilder builder, string serviceName)
    {
        builder.UseSerilog(serviceName);
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tb =>
            {
                tb.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter()
                    .AddOtlpExporter(o => { o.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]?? string.Empty); });
            })
            .WithMetrics(mb =>
            {
                mb.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddPrometheusExporter();
            });

        return builder;
    }
}