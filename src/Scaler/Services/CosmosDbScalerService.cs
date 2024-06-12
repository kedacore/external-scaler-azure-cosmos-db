using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbScalerService : ExternalScaler.ExternalScalerBase
    {
        private readonly ICosmosDbMetricProvider _metricProvider;
        private readonly ILogger<CosmosDbScalerService> _logger;

        public CosmosDbScalerService(ICosmosDbMetricProvider metricProvider, ILogger<CosmosDbScalerService> logger)
        {
            _metricProvider = metricProvider ?? throw new ArgumentNullException(nameof(metricProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            var scalerMetadata = ScalerMetadata.Create(request);

            bool isActive = (await _metricProvider.GetPartitionCountAsync(scalerMetadata)) > 0L;

            _logger.LogInformation("Scaler is {status}", isActive ? "active" : "inactive");
            return new IsActiveResponse { Result = isActive };
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
            var scalerMetadata = ScalerMetadata.Create(request.ScaledObjectRef);

            var response = new GetMetricsResponse();

            response.MetricValues.Add(new MetricValue
            {
                MetricName = scalerMetadata.MetricName,
                MetricValue_ = await _metricProvider.GetPartitionCountAsync(scalerMetadata),
            });

            _logger.LogInformation("Returning metric value {value} for metric {metric}", response.MetricValues[0].MetricValue_, response.MetricValues[0].MetricName);
            return response;
        }

        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            var scalerMetadata = ScalerMetadata.Create(request);

            var response = new GetMetricSpecResponse();

            response.MetricSpecs.Add(new MetricSpec
            {
                MetricName = scalerMetadata.MetricName,
                TargetSize = 1L,
            });

            _logger.LogInformation("Returning target size {size} for metric {metric}", response.MetricSpecs[0].TargetSize, response.MetricSpecs[0].MetricName);
            return Task.FromResult(response);
        }
    }
}
