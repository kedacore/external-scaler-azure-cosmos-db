using System;
using Xunit;
using Keda.CosmosDB.Scaler.Services;
using Microsoft.Extensions.Logging;
using Keda.CosmosDB.Scaler.Protos;
using Grpc.Core;
using Grpc.Core.Testing;
using System.Threading;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using Keda.CosmosDB.Scaler.Repository;

namespace Keda.CosmosDB.Scaler.UnitTest
{
    public class CosmosDBExternalScalerTests
    {
        [Fact]
        public async void IsActiveTest_ThrowsOnMissingMetadata()
        {
            ICosmosDBRepository cosmosDBRepository = new CosmosDBRepository();

            var scaler = new CosmosDBExternalScaler(CreateTestLogger(), cosmosDBRepository);
            ScaledObjectRef objectRef = new ScaledObjectRef();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => scaler.IsActive(objectRef, CreateServerCallContext()));
        }

        [Fact]
        public async void GetMetricsResponse_ThrowsOnMissingMetadata()
        {
            ICosmosDBRepository cosmosDBRepository = new CosmosDBRepository();

            var scaler = new CosmosDBExternalScaler(CreateTestLogger(), cosmosDBRepository);
            ScaledObjectRef objectRef = new ScaledObjectRef();
            GetMetricsRequest request = new GetMetricsRequest()
            {
                ScaledObjectRef = new ScaledObjectRef()
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => scaler.GetMetrics(request, CreateServerCallContext()));
        }

        [Fact]
        public void CreateCosmosDBTriggerMetadata_Succeeds()
        {
            ICosmosDBRepository cosmosDBRepository = new CosmosDBRepository();

            var scaler = new CosmosDBExternalScaler(CreateTestLogger(), cosmosDBRepository);

            var metadata = new MapField<string, string>();
            var testLease = CreateTestLease();
            metadata.Add(Constants.LeasesConnectionStringMetadata, testLease.LeasesCosmosDBConnectionString);
            metadata.Add(Constants.LeaseDatabaseNameMetadata, testLease.LeaseDatabaseName);
            metadata.Add(Constants.LeaseCollectionNameMetadata, testLease.LeaseCollectionName);

            var testTrigger = CreateTestTrigger();
            metadata.Add(Constants.CollectionNameMetadata, testTrigger.CollectionName);
            metadata.Add(Constants.ConnectionStringMetadata, testTrigger.CosmosDBConnectionString);
            metadata.Add(Constants.DatabaseNameMetadata, testTrigger.DatabaseName);
            
            var trigger = scaler.CreateTriggerFromMetadata(metadata);
            Assert.Equal(trigger.CollectionName, testTrigger.CollectionName);
            Assert.Equal(trigger.CosmosDBConnectionString, testTrigger.CosmosDBConnectionString);
            Assert.Equal(trigger.DatabaseName, testTrigger.DatabaseName);
            Assert.Equal(trigger.Lease.LeaseCollectionName, testLease.LeaseCollectionName);
            Assert.Equal(trigger.Lease.LeaseDatabaseName, testLease.LeaseDatabaseName);
            Assert.Equal(trigger.Lease.LeasesCosmosDBConnectionString, testLease.LeasesCosmosDBConnectionString);
        }


        private static ServerCallContext CreateServerCallContext()
        {
            return TestServerCallContext.Create("fooMethod", null, DateTime.UtcNow, new Metadata(), CancellationToken.None, null, null, null, null, null, null);
        }

        private static CosmosDBLease CreateTestLease()
        {
            return new CosmosDBLease("leaseConnection", "leaseDB", "leaseCollection", "leaseCollectionPrefix");
        }

        private static CosmosDBTrigger CreateTestTrigger()
        {
            return new CosmosDBTrigger("connectionString", "databasename", "collectionName", string.Empty);
        }

        private static ILogger<CosmosDBExternalScaler> CreateTestLogger()
        {
            ILoggerFactory loggerFactory = new LoggerFactory();
            return loggerFactory.CreateLogger<CosmosDBExternalScaler>();
        }
    }
}
