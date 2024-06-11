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
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsActive_ReturnsFalseOnZeroPartitions(bool workloadIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(0L);
            IsActiveResponse response = await _cosmosDbScalerService.IsActive(GetScaledObjectRef(workloadIdentity), null);
            Assert.False(response.Result);
        }

        [Theory]
        [InlineData(1L, true)]
        [InlineData(1L, false)]
        [InlineData(100L, true)]
        [InlineData(100L, false)]
        public async Task IsActive_ReturnsFalseOnNonZeroPartitions(long partitionCount, bool workloadIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(partitionCount);
            IsActiveResponse response = await _cosmosDbScalerService.IsActive(GetScaledObjectRef(workloadIdentity), null);
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
        [InlineData(0L, true)]
        [InlineData(0L, false)]
        [InlineData(1L, true)]
        [InlineData(1L, false)]
        [InlineData(100L, true)]
        [InlineData(100L, false)]
        public async Task GetMetrics_ReturnsPartitionCount(long partitionCount, bool workloadIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(partitionCount);
            GetMetricsResponse response = await _cosmosDbScalerService.GetMetrics(GetGetMetricsRequest(workloadIdentity), null);

            Assert.Single(response.MetricValues);

            Assert.Equal(
                "cosmosdb-partitioncount-example2-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricValues[0].MetricName);

            Assert.Equal(partitionCount, response.MetricValues[0].MetricValue_);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("", false)]
        [InlineData("custom-metric-name", true)]
        [InlineData("custom-metric-name", false)]
        public async Task GetMetrics_ReturnsSameMetricNameIfPassed(string requestMetricName, bool workloadIdentity)
        {
            _metricProviderMock.Setup(provider => provider.GetPartitionCountAsync(It.IsAny<ScalerMetadata>())).ReturnsAsync(1L);

            // No assertion with request.MetricName since it is ignored.
            GetMetricsRequest request = GetGetMetricsRequest(workloadIdentity);
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
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetMetricSpec_DoesNotThrowsOnOptionalMetadata(bool workloadIdentity)
        {
            await _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRef(workloadIdentity), null);
        }

        [Theory]
        [InlineData("endpoint", "connection")]
        [InlineData("leaseEndpoint", "leaseConnection")]
        public async Task GetMetricSpec_ThrowsOnMissingConnections(string firstMetadataKey, string secondMetadataKey)
        {
            var exception = await Assert.ThrowsAnyAsync<Exception>(
                () => _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRefWithoutMetadata(firstMetadataKey, secondMetadataKey), null));
            Assert.IsType<JsonSerializationException>(exception.InnerException);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetMetricSpec_ReturnsMetricSpec(bool workloadIdentity)
        {
            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(GetScaledObjectRef(workloadIdentity), null);

            Assert.Single(response.MetricSpecs);

            Assert.Equal(
                "cosmosdb-partitioncount-example2-com-dummy-lease-database-id-dummy-lease-container-id-dummy-processor-name",
                response.MetricSpecs[0].MetricName);

            Assert.Equal(1L, response.MetricSpecs[0].TargetSize);
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("", false)]
        [InlineData("custom-metric-name", true)]
        [InlineData("custom-metric-name", false)]
        public async Task GetMetricSpec_ReturnsSameMetricNameIfPassed(string requestMetricName, bool workloadIdentity)
        {
            ScaledObjectRef request = GetScaledObjectRef(workloadIdentity);
            request.ScalerMetadata["metricName"] = requestMetricName;

            GetMetricSpecResponse response = await _cosmosDbScalerService.GetMetricSpec(request, null);

            Assert.Single(response.MetricSpecs);
            Assert.Equal(requestMetricName, response.MetricSpecs[0].MetricName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetMetricSpec_ReturnsNormalizedMetricName(bool workloadIdentity)
        {
            ScaledObjectRef request = GetScaledObjectRef(workloadIdentity);
            if (workloadIdentity)
            {
                request.ScalerMetadata["leaseEndpoint"] = "https://example.com:443";
            }
            else
            {
                request.ScalerMetadata["leaseConnection"] = "AccountEndpoint=https://example.com:443/;AccountKey=ZHVtbXky";
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

        private static GetMetricsRequest GetGetMetricsRequest(bool workloadIdentity)
        {
            return new GetMetricsRequest
            {
                MetricName = "dummy-metric-name",
                ScaledObjectRef = GetScaledObjectRef(workloadIdentity),
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
            var scaledObjectRef = GetScaledObjectRef(workloadIdentity: true);
            // this is not technically correct but for sake of the test we need both connection and endpoint to be present
            scaledObjectRef.ScalerMetadata["endpoint"] = "https://example1.com:443";
            scaledObjectRef.ScalerMetadata["leaseEndpoint"] = "https://example2.com:443";

            foreach (string metadataKey in metadataKeys)
                scaledObjectRef.ScalerMetadata.Remove(metadataKey);

            return scaledObjectRef;
        }

        private static ScaledObjectRef GetScaledObjectRef(bool workloadIdentity = false)
        {
            var scaledObjectRef = new ScaledObjectRef
            {
                Name = "dummy-scaled-object",
                Namespace = "dummy-namespace",
            };

            MapField<string, string> scalerMetadata = scaledObjectRef.ScalerMetadata;

            if (workloadIdentity)
            {
                scalerMetadata["endpoint"] = "https://example1.com:443";
                scalerMetadata["leaseEndpoint"] = "https://example2.com:443";
            }
            else
            {
                scalerMetadata["connection"] = "AccountEndpoint=https://example1.com:443/;AccountKey=ZHVtbXkx";
                scalerMetadata["leaseConnection"] = "AccountEndpoint=https://example2.com:443/;AccountKey=ZHVtbXky";
            }
            scalerMetadata["databaseId"] = "dummy-database-id";
            scalerMetadata["containerId"] = "dummy-container-id";
            scalerMetadata["leaseDatabaseId"] = "dummy-lease-database-id";
            scalerMetadata["leaseContainerId"] = "dummy-lease-container-id";
            scalerMetadata["processorName"] = "dummy-processor-name";

            return scaledObjectRef;
        }
    }
}
