using samane.Configuration;
using Microsoft.AspNetCore.Cors.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace samane
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddScoped<samane.Services.Interfaces.IDevelopmentSessionService,
                               samane.Services.Implementation.DevelopmentSessionService>();
            services.AddScoped<samane.Services.Interfaces.IAttachmentService,
                               samane.Services.Implementation.AttachmentService>();
            services.AddScoped<samane.Services.Interfaces.IGridPreferenceService,
                               samane.Services.Implementation.GridPreferenceService>();
            services.AddScoped<samane.Services.Interfaces.IApplicantProfileResolver,
                               samane.Services.Implementation.ClaimsApplicantProfileResolver>();
            services.AddScoped<samane.Security.ICurrentActor, samane.Security.CurrentActor>();
            services.AddSingleton<samane.Data.ISqlConnectionFactory, samane.Data.SqlConnectionFactory>();
            services.AddScoped<samane.Services.Interfaces.IPersonnelReviewService,
                               samane.Services.Implementation.PersonnelReviewService>();
            services
                .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults
                                       .AuthenticationScheme)
                .AddCookie(options =>
                           {
                               options.Events.OnRedirectToLogin = c =>
                               {
                                   c.Response.StatusCode = 401;
                                   return Task.CompletedTask;
                               };
                               options.Events.OnRedirectToAccessDenied = c =>
                               {
                                   c.Response.StatusCode = 403;
                                   return Task.CompletedTask;
                               };
                           });
            services.AddSingleton<RuntimeSettings>();
            services.AddCors();
            services.AddSingleton<ICorsPolicyProvider, RuntimeCorsPolicyProvider>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            // CORS must run before redirects and MVC, including OPTIONS requests.
            app.UseMiddleware<samane.Security.ApiExceptionMiddleware>();
            app.UseCors();
            app.UseAuthentication();
            // Settings are public configuration, never secrets; do not cache them.
            app.Use(async (context, next) =>
                    {
                        if (context.Request.Path.StartsWithSegments("/settings"))
                            context.Response.Headers["Cache-Control"] = "no-store";
                        await next();
                    });
            app.UseStaticFiles();
            // HTTPS termination/bindings are configured by the deployment host.
            app.UseMvc();
        }
    }
}
