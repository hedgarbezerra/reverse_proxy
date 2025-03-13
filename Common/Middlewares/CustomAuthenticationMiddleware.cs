using Microsoft.AspNetCore.Http;

namespace Common.Middlewares;


public interface ITokenService
{
    bool IsTokenValid(string token);
    string RenewToken(string refreshToken);
}

public class TokenService : ITokenService
{
    public bool IsTokenValid(string token) => true;

    public string RenewToken(string refreshToken)
    {
        return "new-token";
    }
}

public class CustomAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITokenService _tokenService;

    public CustomAuthenticationMiddleware(RequestDelegate next, ITokenService tokenService)
    {
        _next = next;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if(string.IsNullOrWhiteSpace(authHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            // Verificar se o token é válido
            if (!_tokenService.IsTokenValid(token))
            {
                var refreshToken = context.Request.Headers["Refresh-Token"].FirstOrDefault();
                var newToken = _tokenService.RenewToken(refreshToken);

                if (string.IsNullOrWhiteSpace(newToken))
                {
                    // Se não for possível renovar o token, retorna 401 Unauthorized
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                context.Request.Headers["Authorization"] = $"Bearer {newToken}";
            }
        }

        await _next(context);
    }
}