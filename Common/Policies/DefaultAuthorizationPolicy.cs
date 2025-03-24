using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Policies
{
    public class DefaultAuthorizationPolicy : IAuthorizationPolicyProvider
    {
        public const string Name = "default-authorization-policy";
        private static readonly AuthorizationPolicy _defaultPolicy = new AuthorizationPolicyBuilder(Constants.Jwt.Scheme)
                .RequireAuthenticatedUser()
                .Build();

        private static readonly AuthorizationPolicy _fallbackPolicy = new AuthorizationPolicyBuilder()
                .Build();

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return Task.FromResult(_defaultPolicy);
        }

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return Task.FromResult<AuthorizationPolicy?>(_fallbackPolicy);
        }

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (policyName == Name)
            {
                return Task.FromResult<AuthorizationPolicy?>(_defaultPolicy);
            }

            return Task.FromResult<AuthorizationPolicy?>(null);
        }
    }
}
