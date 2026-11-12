using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace samane.Configuration
{
    public class RuntimeCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly RuntimeSettings settings;
        private readonly ILogger<RuntimeCorsPolicyProvider> logger;
        public RuntimeCorsPolicyProvider(RuntimeSettings settings, ILogger<RuntimeCorsPolicyProvider> logger)
        {
            this.settings = settings;
            this.logger = logger;
        }
        public Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            var builder = new CorsPolicyBuilder();
            try
            {
                var origins = settings.ReadOrigins();
                if (origins.Length > 0)
                    builder.WithOrigins(origins).AllowAnyHeader().WithMethods("GET", "POST", "OPTIONS").AllowCredentials();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Cannot read CORS settings. Cross-origin access is denied.");
            }
            // No preflight cache: edited origins apply to the next request.
            builder.SetPreflightMaxAge(TimeSpan.Zero);
            return Task.FromResult(builder.Build());
        }
    }
}
