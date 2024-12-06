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

        [Fact]
        public async Task IsActive_ReturnsFalseOnZeroPartitions()
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(0L);
            IsActiveResponse response = await _cosmosDbScalerService.IsActive(GetScaledObjectRef(), null);
            Assert.False(response.Result);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(100L)]
        public async Task IsActive_ReturnsFalseOnNonZeroPartitions(long partitionCount)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(partitionCount);
            IsActiveResponse response = await _cosmosDbScalerService.IsActive(GetScaledObjectRef(), null);
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
        [InlineData(0L)]
        [InlineData(1L)]
        [InlineData(100L)]
        public async Task GetMetrics_ReturnsPartitionCount(long partitionCount)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(partitionCount);
            GetMetricsResponse response = await _cosmosDbScalerService.GetMetrics(GetGetMetricsRequest(), null);

            Assert.Single(response.MetricValues);

            Assert.Equal(
                "cosmosdb-partitioncount-example2-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricValues[0].MetricName);

            Assert.Equal(partitionCount, response.MetricValues[0].MetricValue_);
        }

        [Theory]
        [InlineData("")]
        [InlineData("custom-metric-name")]
        public async Task GetMetrics_ReturnsSameMetricNameIfPassed(string requestMetricName)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(1L);

            // No assertion with request.MetricName since it is ignored.
            GetMetricsRequest request = GetGetMetricsRequest();
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

        [Fact]
        public async Task GetMetricSpec_ReturnsMetricSpec()
        {
            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRef(), null);

            Assert.Single(response.MetricSpecs);

            Assert.Equal(
                "cosmosdb-partitioncount-example2-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricSpecs[0].MetricName);

            Assert.Equal(1L, response.MetricSpecs[0].TargetSize);
        }

        [Theory]
        [InlineData("")]
        [InlineData("custom-metric-name")]
        public async Task GetMetricSpec_ReturnsSameMetricNameIfPassed(string requestMetricName)
        {
            ScaledObjectRef request = GetScaledObjectRef();
            request.ScalerMetadata["metricName"] = requestMetricName;

            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(request, null);

            Assert.Single(response.MetricSpecs);
            Assert.Equal(requestMetricName, response.MetricSpecs[0].MetricName);
        }

        [Fact]
        public async Task GetMetricSpec_ReturnsNormalizedMetricName()
        {
            ScaledObjectRef request = GetScaledObjectRef();
            request.ScalerMetadata["leaseConnection"] = "AccountEndpoint=https://example.com:443/;AccountKey=ZHVtbXky";
            request.ScalerMetadata["leaseDatabaseId"] = "Dummy.Lease.Database.Id";
            request.ScalerMetadata["leaseContainerId"] = "Dummy:Lease:Container:Id";
            request.ScalerMetadata["processorName"] = "Dummy%Processor%Name";

            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(request, null);

            Assert.Single(response.MetricSpecs);

            Assert.Equal(
                "cosmosdb-partitioncount-example-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricSpecs[0].MetricName);
        } 

        [Theory]
        [InlineData("connection")]
        [InlineData("connectionFromEnv")]
        [InlineData("leaseConnection")]
        [InlineData("leaseConnectionFromEnv")]
        public async Task IsActive_OnMissingMetadataForEitherOfTheConnections(string metadataKey)
        {
            var scaledObjectRef = GetScaledObjectRefWithoutMetadata(metadataKey);
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(1L);
            IsActiveResponse response = await _cosmosDbScalerService.IsActive(scaledObjectRef, null);
            Assert.True(response.Result);
        }

        [Theory]
        [InlineData("connection", "connectionFromEnv")]
        [InlineData("leaseConnection", "leaseConnectionFromEnv")]
        public async Task IsActive_ThrowsOnMissingConnections(string primaryMetadataKey, string secondaryMetadataKey)
        {
            var scaledObjectRef = GetScaledObjectRef();
            scaledObjectRef.ScalerMetadata.Remove(primaryMetadataKey);
            scaledObjectRef.ScalerMetadata.Remove(secondaryMetadataKey);

            await Assert.ThrowsAsync<JsonSerializationException>(
                () => _cosmosDbScalerService.IsActive(scaledObjectRef, null));
        }

        [Theory]
        [InlineData("connection", "connectionFromEnv")]
        [InlineData("leaseConnection", "leaseConnectionFromEnv")]
        public async Task GetMetrics_ThrowsOnMissingConnections(string primaryMetadataKey, string secondaryMetadataKey)
        {
            var request = GetGetMetricsRequest();
            request.ScaledObjectRef.ScalerMetadata.Remove(primaryMetadataKey);
            request.ScaledObjectRef.ScalerMetadata.Remove(secondaryMetadataKey);

            await Assert.ThrowsAsync<JsonSerializationException>(
                () => _cosmosDbScalerService.GetMetrics(request, null));
        }

        [Theory]
        [InlineData("connection", "connectionFromEnv")]
        [InlineData("leaseConnection", "leaseConnectionFromEnv")]
        public async Task GetMetricSpec_ThrowsOnMissingConnections(string primaryMetadataKey, string secondaryMetadataKey)
        {
            var scaledObjectRef = GetScaledObjectRef();
            scaledObjectRef.ScalerMetadata.Remove(primaryMetadataKey);
            scaledObjectRef.ScalerMetadata.Remove(secondaryMetadataKey);

            await Assert.ThrowsAsync<JsonSerializationException>(
                () => _cosmosDbScalerService.GetMetricSpec(scaledObjectRef, null));
        }

        private static GetMetricsRequest GetGetMetricsRequest()
        {
            return new GetMetricsRequest
            {
                MetricName = "dummy-metric-name",
                ScaledObjectRef = GetScaledObjectRef(),
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

        private static ScaledObjectRef GetScaledObjectRefWithoutMetadata(string metadataKey)
        {
            var scaledObjectRef = GetScaledObjectRef();
            scaledObjectRef.ScalerMetadata.Remove(metadataKey);

            return scaledObjectRef;
        }

        private static ScaledObjectRef GetScaledObjectRef()
        {
            var scaledObjectRef = new ScaledObjectRef
            {
                Name = "dummy-scaled-object",
                Namespace = "dummy-namespace",
            };

            MapField<string, string> scalerMetadata = scaledObjectRef.ScalerMetadata;

            scalerMetadata["connection"] = "AccountEndpoint=https://example1.com:443/;AccountKey=ZHVtbXkx";
            scalerMetadata["connectionFromEnv"] = "dummy-connection-from-env";  //KEDA external scaler will receive the actual connection string.
            scalerMetadata["databaseId"] = "dummy-database-id";
            scalerMetadata["containerId"] = "dummy-container-id";
            scalerMetadata["leaseConnection"] = "AccountEndpoint=https://example2.com:443/;AccountKey=ZHVtbXky";
            scalerMetadata["leaseConnectionFromEnv"] = "dummy-lease-connection-from-env";  //KEDA external scaler will receive the actual connection string.
            scalerMetadata["leaseDatabaseId"] = "dummy-lease-database-id";
            scalerMetadata["leaseContainerId"] = "dummy-lease-container-id";
            scalerMetadata["processorName"] = "dummy-processor-name";

            return scaledObjectRef;
        }
    }
}
