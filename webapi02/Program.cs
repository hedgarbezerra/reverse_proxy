using Common.Extensions;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.UseSerilog("webapi02");
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

app.MapGet("/hello-world", () => Task.FromResult("Hello world form webapi02"))
    .WithName("HelloWorld")
    .WithOpenApi();

app.MapPost("/req", async (HttpContext httpContext) =>
    {
        var requestData = await OpenTelemetryExtensions.GetData(httpContext);
        return Results.Ok(requestData);
    })
    .WithName("GetRequestData")
    .WithOpenApi();

//Ajusta o caminho da requisição do arquivo openapi.json, necessário para o nginx, YARP e ocelot funcionam sem
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swaggerDoc, httpRequest) =>
    {
        if (!httpRequest.Headers.ContainsKey("X-Forwarded-Host"))
            return;
        var basePath = "proxy";
        var serverUrl = $"{httpRequest.Scheme}://{httpRequest.Headers["X-Forwarded-Host"]}/{basePath}";
        swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = serverUrl } };
    });
});
app.UseSwaggerUI();
app.UseAuthorization();
app.UseSerilogRequestLogging(opt => OpenTelemetryExtensions.EnrichDiagnosticContext(opt));
app.UseCors();

app.Run();
