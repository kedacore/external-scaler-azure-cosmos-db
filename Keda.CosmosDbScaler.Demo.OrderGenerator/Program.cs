using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Keda.CosmosDbScaler.Demo.Shared;
using Microsoft.Azure.Cosmos;
using Database = Microsoft.Azure.Cosmos.Database;

namespace Keda.CosmosDbScaler.Demo.OrderGenerator
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length != 1 || !new[] {"run", "setup", "teardown"}.Contains(args[0]))
            {
                Console.WriteLine();
                Console.WriteLine("Please use one of the following verbs with the command:");
                Console.WriteLine("    run      : Add orders within the order-container");
                Console.WriteLine("    setup    : Create Cosmos database and order-container");
                Console.WriteLine("    teardown : Delete Cosmos database and containers inside");
                Console.WriteLine();
                return;
            }

            switch (args[0])
            {
                case "run": await RunAsync(); break;
                case "setup": await SetupAsync(); break;
                case "teardown": await TeardownAsync(); break;
            }
        }

        private static async Task RunAsync()
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
            Container container = new CosmosClient(AssetInfo.Connection)
                .GetDatabase(AssetInfo.DatabaseId)
                .GetContainer(AssetInfo.ContainerId);

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
            Console.WriteLine($"Creating database: {AssetInfo.DatabaseId}");

            Database database = await new CosmosClient(AssetInfo.Connection)
                .CreateDatabaseIfNotExistsAsync(AssetInfo.DatabaseId);

            Console.WriteLine($"Creating container: {AssetInfo.ContainerId}");

            Container container = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties(AssetInfo.ContainerId, partitionKeyPath: "/id"),
                throughput: 400);

            Console.WriteLine("Done!");
        }

        private static async Task TeardownAsync()
        {
            var client = new CosmosClient(AssetInfo.Connection);

            try
            {
                Console.WriteLine($"Deleting database: {AssetInfo.DatabaseId}");
                await client.GetDatabase(AssetInfo.DatabaseId).DeleteAsync();
            }
            catch (CosmosException)
            {
                Console.WriteLine("Database does not exist");
            }

            Console.WriteLine("Done!");
        }
    }
}
