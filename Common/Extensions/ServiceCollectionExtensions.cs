using Common.Policies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            //services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicy>();

            services.AddAuthentication(Constants.Jwt.Scheme)
            .AddJwtBearer(Constants.Jwt.Scheme, jwtOptions =>
            {
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Constants.Jwt.Issuer,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                };
                jwtOptions.SaveToken = true;
                jwtOptions.MapInboundClaims = false;
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(DefaultAuthorizationPolicy.Name, 
                    policy => policy.AddAuthenticationSchemes(Constants.Jwt.Scheme)
                                    .RequireClaim(ClaimTypes.System, Constants.Jwt.Claims.System)
                                    .RequireAuthenticatedUser());
            });

            return services;
        }
    }
}
