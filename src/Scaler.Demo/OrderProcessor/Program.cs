using Keda.CosmosDb.Scaler.Demo.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDb.Scaler.Demo.OrderProcessor
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => builder.AddSimpleConsole(options => options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton(CosmosDbConfig.Create(hostContext.Configuration));
                });
    }
}
