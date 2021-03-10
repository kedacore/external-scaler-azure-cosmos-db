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

namespace Keda.CosmosDB.Scaler.UnitTest
{
    public class CosmosDBExternalScalerTests
    {
        [Fact]
        public async void IsActiveTest_ThrowsOnMissingMetadata()
        {
            ILoggerFactory loggerFactory = new LoggerFactory();
            var scaler = new CosmosDBExternalScaler(loggerFactory);
            ScaledObjectRef objectRef = new ScaledObjectRef();
            await Assert.ThrowsAsync<KeyNotFoundException>(() => scaler.IsActive(objectRef, CreateServerCallContext()));
        }

        [Fact]
        public async void GetMetricsResponse_ThrowsOnMissingMetadata()
        {
            ILoggerFactory loggerFactory = new LoggerFactory();
            var scaler = new CosmosDBExternalScaler(loggerFactory);
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
            ILoggerFactory loggerFactory = new LoggerFactory();
            var scaler = new CosmosDBExternalScaler(loggerFactory);

            var metadata = new MapField<string, string>();
            var testLease = CreateTestLease();
            metadata.Add(CosmosDBLeaseMetadata.LeasesCosmosDBConnectionString, testLease.LeasesCosmosDBConnectionString);
            metadata.Add(CosmosDBLeaseMetadata.LeaseDatabaseName, testLease.LeaseDatabaseName);
            metadata.Add(CosmosDBLeaseMetadata.LeaseCollectionName, testLease.LeaseCollectionName);

            var testTrigger = CreateTestTrigger();
            metadata.Add(CosmosDBTriggerMetadata.CollectionName, testTrigger.CollectionName);
            metadata.Add(CosmosDBTriggerMetadata.CosmosDBConnectionString, testTrigger.CosmosDBConnectionString);
            metadata.Add(CosmosDBTriggerMetadata.DatabaseName, testTrigger.DatabaseName);
            
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
            return new CosmosDBLease("leaseConnection", "leaseDB", "leaseCollection");
        }

        private static CosmosDBTrigger CreateTestTrigger()
        {
            return new CosmosDBTrigger("connectionString", "databasename", "collectionName", string.Empty);
        }
    }
}
