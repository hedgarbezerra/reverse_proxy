using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Policies
{
    public class CorsDefaultPolicy : ICorsPolicyProvider
    {
        public const string Name = "cors-default-policy";

        private static CorsPolicyBuilder _policyBuilder = new CorsPolicyBuilder()
            .AllowAnyOrigin()
            .WithExposedHeaders("X-Cors-Pragma")
            .AllowAnyMethod()
            .AllowAnyHeader();
        public static CorsPolicy CorsPolicy => _policyBuilder.Build();
        public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName) => Task.FromResult<CorsPolicy?>(_policyBuilder.Build());
    }
}
