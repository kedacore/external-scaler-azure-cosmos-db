using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Keda.CosmosDbScaler.Demo.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDbScaler.Demo.OrderProcessor
{
    internal sealed class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private ChangeFeedProcessor _processor;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken stoppingToken)
        {
            Container leaseContainer = await new CosmosClient(AssetInfo.LeaseConnection)
                .GetDatabase(AssetInfo.LeaseDatabaseId)
                .CreateContainerIfNotExistsAsync(
                    containerProperties: new ContainerProperties(AssetInfo.LeaseContainerId, "/id"),
                    throughput: 400);

            // Change feed processor instance name should be unique for each container application.
            string instanceName = $"Instance-{Dns.GetHostName()}";

            _processor = new CosmosClient(AssetInfo.Connection)
                .GetDatabase(AssetInfo.DatabaseId)
                .GetContainer(AssetInfo.ContainerId)
                .GetChangeFeedProcessorBuilder<Order>(AssetInfo.ProcessorName, ProcessOrdersAsync)
                .WithInstanceName(instanceName)
                .WithLeaseContainer(leaseContainer)
                .Build();

            await _processor.StartAsync();
            _logger.LogInformation($"Started change feed processor instance {instanceName}");
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await _processor.StopAsync();
            _logger.LogInformation("Stopped change feed processor");

            await base.StopAsync(stoppingToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        private async Task ProcessOrdersAsync(IReadOnlyCollection<Order> orders, CancellationToken cancellationToken)
        {
            _logger.LogInformation(orders.Count + " orders received");
            var tasks = new List<Task>();

            foreach (Order order in orders)
            {
                _logger.LogInformation($"Processing order {order.Id} for {order.Amount} unit(s) of {order.ArticleNumber} bought by {order.Customer.FirstName} {order.Customer.LastName}");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                _logger.LogInformation($"Order {order.Id} processed");
            }
        }
    }
}
