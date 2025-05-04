using Common;
using Common.Extensions;
using Common.Policies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;


var builder = WebApplication.CreateBuilder(args);

builder.UseSerilog("webapi01");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(2, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("x-api-version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
    });

builder.Services.AddSwaggerGen(options =>
{
    var provider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();

    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(
            description.GroupName,
            new OpenApiInfo
            {
                Title = $"WebAPI {description.ApiVersion}",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated ? "Esta versão está depreciada." : string.Empty
            });
    }
});

builder.Services.AddCors(options => { options.AddPolicy(CorsDefaultPolicy.Name, CorsDefaultPolicy.CorsPolicy); });

var app = builder.Build();

#region V1 - Depreciada

var v1 = app.NewVersionedApi()
    .MapGroup("/v{version:apiVersion}")
    .HasApiVersion(new ApiVersion(1, 0));

v1.MapGet("/hello-world", () => Task.FromResult("Hello world form webapi01"))
    .WithName("HelloWorld")
    .WithOpenApi();

v1.MapPost("/req", async (HttpContext httpContext) =>
    {
        var requestData = await OpenTelemetryExtensions.GetData(httpContext);
        return Results.Ok(requestData);
    })
    .WithName("GetRequestData")
    .WithOpenApi();

v1.MapPost("/login", (LoginRequest req) =>
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

#endregion

#region V2

var v2 = app.NewVersionedApi()
    .MapGroup("/v{version:apiVersion}")
    .HasApiVersion(new ApiVersion(2, 0));

v2.MapGet("/hello-world", () => Results.Ok(new { message = "Hello World from v2" }))
    .WithName("HelloWorldV2");

#endregion

//Ajusta o caminho da requisição do arquivo openapi.json, necessário para o nginx, YARP e ocelot funcionam sem
// app.UseSwagger(c =>
// {
//     c.PreSerializeFilters.Add((swaggerDoc, httpRequest) =>
//     {
//         if (!httpRequest.Headers.ContainsKey("X-Forwarded-Host"))
//             return;
//         var basePath = "proxy";
//         var serverUrl = $"{httpRequest.Scheme}://{httpRequest.Headers["X-Forwarded-Host"]}/{basePath}";
//         swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = serverUrl } };
//     });
// });

app.UseCors(CorsDefaultPolicy.Name);
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    foreach (var description in app.DescribeApiVersions())
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            $"WebAPI {description.GroupName.ToUpperInvariant()}");
    }
});
app.UseSerilogRequestLogging();

await app.RunAsync();


record LoginRequest(string Username, string Password);