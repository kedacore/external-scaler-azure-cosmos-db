using System;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Keda.CosmosDb.Scaler.Tests
{
    public class CosmosDbScalerServiceTests
    {
        private readonly CosmosDbScalerService _cosmosDbScalerService;
        private readonly Mock<ICosmosDbMetricProvider> _metricProviderMock;

        public CosmosDbScalerServiceTests()
        {
            _metricProviderMock = new Mock<ICosmosDbMetricProvider>();
            _cosmosDbScalerService = new CosmosDbScalerService(_metricProviderMock.Object);
        }

        [Theory]
        [InlineData("databaseId")]
        [InlineData("containerId")]
        [InlineData("leaseDatabaseId")]
        [InlineData("leaseContainerId")]
        [InlineData("processorName")]
        public async Task IsActive_ThrowsOnMissingMetadata(string metadataKey)
        {
            await Assert.ThrowsAsync<JsonSerializationException>(
                () => _cosmosDbScalerService.IsActive(GetScaledObjectRefWithoutMetadata(metadataKey), null));
        }

        [Theory]
        [InlineData("endpoint", "connectionFromEnv")]
        [InlineData("leaseEndpoint", "leaseConnectionFromEnv")]
        public async Task IsActive_ThrowsOnMissingConnections(string endpointKey, string connectionkey)
        {
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRefWithoutMetadata(endpointKey, connectionkey), null));
            Assert.IsType<JsonSerializationException>(ex.InnerException);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task IsActive_ReturnsFalseOnZeroPartitions(bool useManagedIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(0L);
            IsActiveResponse response = await _cosmosDbScalerService.IsActive(GetScaledObjectRef(useManagedIdentity), null);
            Assert.False(response.Result);
        }

        [Theory]
        [InlineData(1L, false)]
        [InlineData(1L, true)]
        [InlineData(100L, false)]
        [InlineData(100L, true)]
        public async Task IsActive_ReturnsTrueOnNonZeroPartitions(long partitionCount, bool useManagedIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(partitionCount);
            IsActiveResponse response = await _cosmosDbScalerService.IsActive(GetScaledObjectRef(useManagedIdentity), null);
            Assert.True(response.Result);
        }

        [Theory]
        [InlineData("databaseId")]
        [InlineData("containerId")]
        [InlineData("leaseDatabaseId")]
        [InlineData("leaseContainerId")]
        [InlineData("processorName")]
        public async Task GetMetrics_ThrowsOnMissingMetadata(string metadataKey)
        {
            await Assert.ThrowsAsync<JsonSerializationException>(
                () => _cosmosDbScalerService.GetMetrics(GetGetMetricsRequestWithoutMetadata(metadataKey), null));
        }


        [Theory]
        [InlineData("endpoint", "connectionFromEnv")]
        [InlineData("leaseEndpoint", "leaseConnectionFromEnv")]
        public async Task GetMetrics_ThrowsOnMissingConnections(string endpointKey, string connectionkey)
        {
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRefWithoutMetadata(endpointKey, connectionkey), null));
            Assert.IsType<JsonSerializationException>(ex.InnerException);
        }

        [Theory]
        [InlineData(0L, false)]
        [InlineData(0L, true)]
        [InlineData(1L, false)]
        [InlineData(1L, true)]
        [InlineData(100L, false)]
        [InlineData(100L, true)]
        public async Task GetMetrics_ReturnsPartitionCount(long partitionCount, bool useManagedIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(partitionCount);
            GetMetricsResponse response = await _cosmosDbScalerService.GetMetrics(GetGetMetricsRequest(useManagedIdentity), null);

            Assert.Single(response.MetricValues);

            Assert.Equal(
                "cosmosdb-partitioncount-example2-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricValues[0].MetricName);

            Assert.Equal(partitionCount, response.MetricValues[0].MetricValue_);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("", true)]
        [InlineData("custom-metric-name", false)]
        [InlineData("custom-metric-name", true)]
        public async Task GetMetrics_ReturnsSameMetricNameIfPassed(string requestMetricName, bool useManagedIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(1L);

            // No assertion with request.MetricName since it is ignored.
            GetMetricsRequest request = GetGetMetricsRequest(useManagedIdentity);
            request.ScaledObjectRef.ScalerMetadata["metricName"] = requestMetricName;

            GetMetricsResponse response = await _cosmosDbScalerService.GetMetrics(request, null);

            Assert.Single(response.MetricValues);
            Assert.Equal(requestMetricName, response.MetricValues[0].MetricName);
        }

        [Theory]
        [InlineData("databaseId")]
        [InlineData("containerId")]
        [InlineData("leaseDatabaseId")]
        [InlineData("leaseContainerId")]
        [InlineData("processorName")]
        public async Task GetMetricSpec_ThrowsOnMissingMetadata(string metadataKey)
        {
            await Assert.ThrowsAsync<JsonSerializationException>(
                () => _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRefWithoutMetadata(metadataKey), null));
        }

        [Theory]
        [InlineData("endpoint", "connectionFromEnv")]
        [InlineData("leaseEndpoint", "leaseConnectionFromEnv")]
        public async Task GetMetricSpec_ThrowsOnMissingConnections(string endpointKey, string connectionkey)
        {
            var ex = await Assert.ThrowsAnyAsync<Exception>(
                () => _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRefWithoutMetadata(endpointKey, connectionkey), null));
            Assert.IsType<JsonSerializationException>(ex.InnerException);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetMetricSpec_ReturnsMetricSpec(bool useManagedIdentity)
        {
            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRef(useManagedIdentity), null);

            Assert.Single(response.MetricSpecs);

            Assert.Equal(
                "cosmosdb-partitioncount-example2-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricSpecs[0].MetricName);

            Assert.Equal(1L, response.MetricSpecs[0].TargetSize);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("", true)]
        [InlineData("custom-metric-name", false)]
        [InlineData("custom-metric-name", true)]
        public async Task GetMetricSpec_ReturnsSameMetricNameIfPassed(string requestMetricName, bool useManagedIdentity)
        {
            ScaledObjectRef request = GetScaledObjectRef(useManagedIdentity);
            request.ScalerMetadata["metricName"] = requestMetricName;

            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(request, null);

            Assert.Single(response.MetricSpecs);
            Assert.Equal(requestMetricName, response.MetricSpecs[0].MetricName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetMetricSpec_ReturnsNormalizedMetricName(bool useManagedIdentity)
        {
            ScaledObjectRef request = GetScaledObjectRef(useManagedIdentity);
            if (useManagedIdentity)
            {
                request.ScalerMetadata["leaseEndpoint"] = "https://example.com:443/";
            } else
            {
                request.ScalerMetadata["leaseConnectionFromEnv"] = "AccountEndpoint=https://example.com:443/;AccountKey=ZHVtbXky";
            }
            request.ScalerMetadata["leaseDatabaseId"] = "Dummy.Lease.Database.Id";
            request.ScalerMetadata["leaseContainerId"] = "Dummy:Lease:Container:Id";
            request.ScalerMetadata["processorName"] = "Dummy%Processor%Name";

            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(request, null);

            Assert.Single(response.MetricSpecs);

            Assert.Equal(
                "cosmosdb-partitioncount-example-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricSpecs[0].MetricName);
        }

        private static GetMetricsRequest GetGetMetricsRequest(bool useManagedIdentity = false)
        {
            return new GetMetricsRequest
            {
                MetricName = "dummy-metric-name",
                ScaledObjectRef = GetScaledObjectRef(useManagedIdentity),
            };
        }

        private static GetMetricsRequest GetGetMetricsRequestWithoutMetadata(string metadataKey)
        {
            return new GetMetricsRequest
            {
                MetricName = "dummy-metric-name",
                ScaledObjectRef = GetScaledObjectRefWithoutMetadata(metadataKey),
            };
        }

        private static ScaledObjectRef GetScaledObjectRefWithoutMetadata(params string[] metadataKeys)
        {
            var scaledObjectRef = GetScaledObjectRef();
            foreach(var key in metadataKeys)
            {
                scaledObjectRef.ScalerMetadata.Remove(key);
            }

            return scaledObjectRef;
        }

        private static ScaledObjectRef GetScaledObjectRef(bool useManagedIdentity = false)
        {
            var scaledObjectRef = new ScaledObjectRef
            {
                Name = "dummy-scaled-object",
                Namespace = "dummy-namespace",
            };

            MapField<string, string> scalerMetadata = scaledObjectRef.ScalerMetadata;

            scalerMetadata["databaseId"] = "dummy-database-id";
            scalerMetadata["containerId"] = "dummy-container-id";
            scalerMetadata["leaseDatabaseId"] = "dummy-lease-database-id";
            scalerMetadata["leaseContainerId"] = "dummy-lease-container-id";
            scalerMetadata["processorName"] = "dummy-processor-name";

            if (useManagedIdentity)
            {
                scalerMetadata["endpoint"] = "https://example1.com:443/";
                scalerMetadata["leaseEndpoint"] = "https://example2.com:443/";
            } else
            {
                scalerMetadata["connectionFromEnv"] = "AccountEndpoint=https://example1.com:443/;AccountKey=ZHVtbXkx";
                scalerMetadata["leaseConnectionFromEnv"] = "AccountEndpoint=https://example2.com:443/;AccountKey=ZHVtbXky";
            }

            return scaledObjectRef;
        }
    }
}
