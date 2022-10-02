using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // Required by mock generator.
[assembly: InternalsVisibleTo("Keda.CosmosDb.Scaler.Tests")]

namespace Keda.CosmosDb.Scaler
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder => builder.AddSimpleConsole(options => options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(kestrelServerOptions =>
                    {
                        // Setup a HTTP/2 endpoint without TLS.
                        kestrelServerOptions.ListenAnyIP(port: 4050, listOptions => listOptions.Protocols = HttpProtocols.Http2);
                    });

                    webBuilder.UseStartup<Startup>();
                });
    }
}
