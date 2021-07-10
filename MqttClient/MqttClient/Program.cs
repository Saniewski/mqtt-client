using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;

namespace MqttClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args);

#if !DEBUG
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                hostBuilder.UseWindowsService();
            else
                hostBuilder.UseSystemd();
#endif

            hostBuilder
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.Sources.Clear();

                    var env = hostingContext.HostingEnvironment;

                    config.SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(logger =>
                    {
                        logger.ClearProviders();
                        logger.SetMinimumLevel(LogLevel.Trace);
                        logger.AddNLog();
                    });

                    services.AddHostedService<MqttWorker>();
                    services.AddSingleton<MqttClient>();
                });

            return hostBuilder;
        }
    }
}
