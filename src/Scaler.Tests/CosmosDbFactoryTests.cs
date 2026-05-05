using System.Threading;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Keda.CosmosDb.Scaler.Tests
{
    public class CosmosDbFactoryTests
    {
        private const string DummyConnection1 = "AccountEndpoint=https://example1.com:443/;AccountKey=ZHVtbXkx";
        private const string DummyConnection2 = "AccountEndpoint=https://example2.com:443/;AccountKey=ZHVtbXky";

        [Fact]
        public void GetCosmosClient_OnlyConstructsOnceForSameKey()
        {
            var factory = new CountingCosmosDbFactory();

            CosmosClient c1 = factory.GetCosmosClient(DummyConnection1, useCredentials: false, clientId: null);
            CosmosClient c2 = factory.GetCosmosClient(DummyConnection1, useCredentials: false, clientId: null);
            CosmosClient c3 = factory.GetCosmosClient(DummyConnection1, useCredentials: false, clientId: null);

            Assert.Same(c1, c2);
            Assert.Same(c2, c3);
            Assert.Equal(1, factory.CreateCount);
        }

        [Fact]
        public void GetCosmosClient_ConstructsOncePerDistinctKey()
        {
            var factory = new CountingCosmosDbFactory();

            factory.GetCosmosClient(DummyConnection1, useCredentials: false, clientId: null);
            factory.GetCosmosClient(DummyConnection2, useCredentials: false, clientId: null);
            factory.GetCosmosClient(DummyConnection1, useCredentials: false, clientId: null);
            factory.GetCosmosClient(DummyConnection2, useCredentials: false, clientId: null);

            Assert.Equal(2, factory.CreateCount);
        }

        private sealed class CountingCosmosDbFactory : CosmosDbFactory
        {
            public int CreateCount;

            protected internal override CosmosClient CreateCosmosClient(
                string endpointOrConnection, bool useCredentials, string clientId)
            {
                Interlocked.Increment(ref CreateCount);
                return base.CreateCosmosClient(endpointOrConnection, useCredentials, clientId);
            }
        }
    }
}
