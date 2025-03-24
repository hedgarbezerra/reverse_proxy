using Common;
using Common.Extensions;
using Common.Policies;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

builder.UseSerilog("webapi01");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsDefaultPolicy.Name, CorsDefaultPolicy.CorsPolicy);
});

var app = builder.Build();


app.MapGet("/hello-world", () => Task.FromResult("Hello world form webapi01"))
    .WithName("HelloWorld")
    .WithOpenApi();

app.MapPost("/req", async (HttpContext httpContext) =>
    {
        var requestData = await OpenTelemetryExtensions.GetData(httpContext);
        return Results.Ok(requestData);
    })
    .WithName("GetRequestData")
    .WithOpenApi();


app.MapPost("/login", (LoginRequest req) =>
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity([
            new Claim(ClaimTypes.Name, req.Username),
            new Claim(ClaimTypes.System, Constants.Jwt.Claims.System),
            ]),
        Expires = DateTime.UtcNow.AddMinutes(3),
        NotBefore = DateTime.UtcNow,
        Issuer = "webapi01",
        IssuedAt = DateTime.UtcNow,
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Constants.Jwt.ApiKey),
            SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwtToken = tokenHandler.WriteToken(token);

    return Results.Ok(new { token = jwtToken, expiracy = tokenDescriptor.Expires });
})
    .WithName("Login")
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
app.UseSerilogRequestLogging();
app.UseCors();

await app.RunAsync();


record LoginRequest(string Username, string Password);