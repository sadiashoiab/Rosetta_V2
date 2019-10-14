using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Rosetta.Services;

namespace Rosetta
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var webHost = CreateWebHostBuilder(args).Build();

            using (var scope = webHost.Services.CreateScope())
            {
                // get a rosettastoneservice instance
                var rosettaStoneService = scope.ServiceProvider.GetRequiredService<IRosettaStoneService>();

                // warm the cache by calling get agencies before the service starts accepting requests
                var _ = await rosettaStoneService.GetAgencies();
            }

            // start accepting requests
            await webHost.RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                // todo: remove this if the port forward works
                // note: pull the port from config rather than hard code would be ideal, but seeing as we are replacing an existing service
                //       where existing clients are expecting to hit the port, keeping it here for now.
                //.UseUrls("https://*:4567")
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var environment = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", false, true);
                    // note: set ASPNETCORE_ENVIRONMENT environment variable to pull in environment specific configurations.
                    //       depending on what OS you are deploying to, this CAN be case sensitive
                    config.AddJsonFile($"appsettings.{environment.EnvironmentName}.json", true, true);
                    config.AddEnvironmentVariables("APPLICATION_");
                })
                .ConfigureLogging((hostingContext, builder) =>
                {
                    builder.AddApplicationInsights(hostingContext.Configuration.GetSection("AI_KEY").Value);
                    builder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Debug);
                    builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Debug);
                    builder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    builder.AddConsole();
                    builder.AddDebug();
                });
    }
}
