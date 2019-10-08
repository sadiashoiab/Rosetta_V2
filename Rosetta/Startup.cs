using System;
using System.Diagnostics.CodeAnalysis;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Polly;
using Rosetta.ActionFilters;
using Rosetta.HealthChecks;
using Rosetta.Services;

namespace Rosetta
{
    [ExcludeFromCodeCoverage]
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
            // todo: add in application insights

            // todo: add application insights collector and publisher
            services.AddHealthChecks()
                .AddCheck<ClearCareOnlineApiHealthCheck>("ClearCare Online API");
                //.AddCheck<RandomHealthCheck>("Random Check");

            services.AddHealthChecksUI();

            services.AddLazyCache();

            services
                .AddHttpClient("BearerTokenHttpClient",
                    client => { client.Timeout = System.Threading.Timeout.InfiniteTimeSpan; })
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1)));

            services
                .AddHttpClient("ClearCareHttpClient",
                    client => { client.Timeout = System.Threading.Timeout.InfiniteTimeSpan; })
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1)));

            services.AddControllers(configure =>
            {
                configure.Filters.Add<IpAddressCaptureActionFilter>(); 
            });

            services.AddSwaggerGen(c => {  
                c.SwaggerDoc("v1", new OpenApiInfo {  
                    Version = "v1",  
                    Title = "RosettaStone API",  
                    Description = "RosettaStone ASP.NET Core Web API"  
                });  
            });

            services.AddScoped<IBearerTokenProvider, BearerTokenProvider>();
            services.AddScoped<IResourceLoader, ResourceLoader>();
            services.AddScoped<IMapper<AgencyFranchiseMap>, AgencyMapper>();
            services.AddScoped<IRosettaStoneService, RosettaStoneService>();

            services.AddSingleton<IIpAddressCaptureService, IpAddressCaptureService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecksUI();

                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "RosettaStone API V1");
            });
        }
    }
}
