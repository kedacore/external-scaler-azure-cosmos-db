using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Keda.CosmosDbScaler.Demo.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Database = Microsoft.Azure.Cosmos.Database;

namespace Keda.CosmosDbScaler.Demo.OrderGenerator
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
            Console.WriteLine("Let's queue some orders, how many do you want?");

            int amount = ReadOrderAmount();
            await CreateOrdersAsync(amount);

            Console.WriteLine("That's it, see you later!");
        }

        private static int ReadOrderAmount()
        {
            int amount;

            while (!int.TryParse(Console.ReadLine(), out amount) || amount < 1 || amount > 10000)
            {
                Console.WriteLine("That's not a valid amount. Please enter a number between 1 and 10000.");
            }

            return amount;
        }

        private static async Task CreateOrdersAsync(int amount)
        {


            Container container = new CosmosClient(_cosmosDbConfig.Connection)
                .GetDatabase(_cosmosDbConfig.DatabaseId)
                .GetContainer(_cosmosDbConfig.ContainerId);

            int remainingAmount = amount;

            while (remainingAmount > 0)
            {
                int newAmount = Math.Min(remainingAmount, 20);

                Task[] createOrderTasks = Enumerable.Range(0, newAmount).Select(_ => CreateOrderAsync(container)).ToArray();

                await Task.WhenAll(createOrderTasks);
                await Task.Delay(TimeSpan.FromSeconds(2));

                remainingAmount -= newAmount;
            }
        }

        private static async Task CreateOrderAsync(Container container)
        {
            var order = GenerateOrder();
            Console.WriteLine($"Creating order {order.Id} - A {order.ArticleNumber} for {order.Customer.FirstName} {order.Customer.LastName}.");
            await container.CreateItemAsync(order);
        }

        private static Order GenerateOrder()
        {
            var customerGenerator = new Faker<Customer>()
                .RuleFor(customer => customer.FirstName, faker => faker.Name.FirstName())
                .RuleFor(customer => customer.LastName, faker => faker.Name.LastName());

            var orderGenerator = new Faker<Order>()
                .RuleFor(order => order.Customer, () => customerGenerator)
                .RuleFor(order => order.Amount, faker => faker.Random.Number(1, 10))
                .RuleFor(order => order.ArticleNumber, faker => faker.Commerce.Product());

            return orderGenerator;
        }

        private static async Task SetupAsync()
        {
            Console.WriteLine($"Creating database {_cosmosDbConfig.DatabaseId}");

            Database database = await new CosmosClient(_cosmosDbConfig.Connection)
                .CreateDatabaseIfNotExistsAsync(_cosmosDbConfig.DatabaseId);

            Console.WriteLine($"Creating container: {_cosmosDbConfig.ContainerId}");

            Container container = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_cosmosDbConfig.ContainerId, partitionKeyPath: "/id"),
                throughput: 400);

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
