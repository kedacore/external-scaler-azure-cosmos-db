using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Bogus.DataSets;
using Keda.CosmosDb.Scaler.Demo.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Database = Microsoft.Azure.Cosmos.Database;

namespace Keda.CosmosDb.Scaler.Demo.OrderGenerator
{
    internal static class Program
    {
        private static CosmosDbConfig _cosmosDbConfig;

        public static async Task Main(string[] args)
        {
            if (args.Length != 1 || !new[] { "generate", "setup", "teardown" }.Contains(args[0]))
            {
                Console.WriteLine();
                Console.WriteLine("Please use one of the following verbs with the command:");
                Console.WriteLine("    generate : Add new orders to the order-container");
                Console.WriteLine("    setup    : Create Cosmos database and order-container");
                Console.WriteLine("    teardown : Delete Cosmos database and containers inside");
                Console.WriteLine();
                return;
            }

            // _cosmosDbConfig should be initialized once the host is built.
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder => _cosmosDbConfig = CosmosDbConfig.Create(builder.Build()))
                .Build();

            switch (args[0])
            {
                case "generate": await GenerateAsync(); break;
                case "setup": await SetupAsync(); break;
                case "teardown": await TeardownAsync(); break;
            }
        }

        private static async Task GenerateAsync()
        {
            int count = ReadOrderCount();
            bool isSingleArticle = ReadIsSingleArticle();
            await CreateOrdersAsync(count, isSingleArticle);
        }

        private static int ReadOrderCount()
        {
            while (true)
            {
                Console.Write("Let's queue some orders, how many do you want? ");

                if (int.TryParse(Console.ReadLine(), out int count) && count >= 1 && count <= 10000)
                {
                    return count;
                }

                Console.WriteLine("That's not a valid amount. Please enter a number between 1 and 10000.");
            }
        }

        private static bool ReadIsSingleArticle()
        {
            bool? isSingleArticle = null;

            while (isSingleArticle == null)
            {
                Console.Write("Do you want to limit orders to single article (to put them in a single partition)? (Y/N) ");

                isSingleArticle = Console.ReadKey().Key switch
                {
                    ConsoleKey.Y => true,
                    ConsoleKey.N => false,
                    _ => null,
                };

                Console.WriteLine();
            }

            return isSingleArticle.Value;
        }

        private static async Task CreateOrdersAsync(int count, bool isSingleArticle)
        {
            Container container = new CosmosClient(_cosmosDbConfig.Connection)
                .GetContainer(_cosmosDbConfig.DatabaseId, _cosmosDbConfig.ContainerId);

            int remainingCount = count;
            string article = isSingleArticle ? new Commerce().Product() : null;

            while (remainingCount > 0)
            {
                // Do not push all orders together as that may cause requests to get throttled.
                int newCount = Math.Min(remainingCount, 20);

                Task[] createOrderTasks = Enumerable.Range(0, newCount)
                    .Select(_ => CreateOrderAsync(container, article))
                    .ToArray();

                await Task.WhenAll(createOrderTasks);
                await Task.Delay(TimeSpan.FromSeconds(2));

                remainingCount -= newCount;
            }

            Console.WriteLine("That's it, see you later!");
        }

        private static async Task CreateOrderAsync(Container container, string article)
        {
            Customer customer = new Faker<Customer>()
                .RuleFor(customer => customer.FirstName, faker => faker.Name.FirstName())
                .RuleFor(customer => customer.LastName, faker => faker.Name.LastName());

            Order order = new Faker<Order>()
                .RuleFor(order => order.Customer, () => customer)
                .RuleFor(order => order.Amount, faker => faker.Random.Number(1, 10))
                .RuleFor(order => order.Article, faker => article ?? faker.Commerce.Product());

            Console.WriteLine($"Creating order {order.Id} - {order.Amount} unit(s) of {order.Article} for {order.Customer.FirstName} {order.Customer.LastName}");
            await container.CreateItemAsync(order);
        }

        private static async Task SetupAsync()
        {
            Console.WriteLine($"Creating database: {_cosmosDbConfig.DatabaseId}");

            Database database = await new CosmosClient(_cosmosDbConfig.Connection)
                .CreateDatabaseIfNotExistsAsync(_cosmosDbConfig.DatabaseId);

            Console.WriteLine($"Creating container: {_cosmosDbConfig.ContainerId} with throughput: {_cosmosDbConfig.ContainerThroughput} RU/s");

            await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_cosmosDbConfig.ContainerId, partitionKeyPath: $"/{nameof(Order.Article)}"),
                throughput: _cosmosDbConfig.ContainerThroughput);

            Console.WriteLine("Done!");
        }

        private static async Task TeardownAsync()
        {
            var client = new CosmosClient(_cosmosDbConfig.Connection);

            try
            {
                Console.WriteLine($"Deleting database: {_cosmosDbConfig.DatabaseId}");
                await client.GetDatabase(_cosmosDbConfig.DatabaseId).DeleteAsync();
            }
            catch (CosmosException)
            {
                Console.WriteLine("Database does not exist");
            }

            Console.WriteLine("Done!");
        }
    }
}