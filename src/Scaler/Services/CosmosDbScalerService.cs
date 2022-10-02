using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Keda.CosmosDb.Scaler
{
    internal sealed class CosmosDbScalerService : ExternalScaler.ExternalScalerBase
    {
        private readonly ICosmosDbMetricProvider _metricProvider;

        public CosmosDbScalerService(ICosmosDbMetricProvider metricProvider)
        {
            _metricProvider = metricProvider ?? throw new ArgumentNullException(nameof(metricProvider));
        }

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            var scalerMetadata = ScalerMetadata.Create(request);

            bool isActive = (await _metricProvider.GetPartitionCountAsync(scalerMetadata)) > 0L;
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

            return Task.FromResult(response);
        }
    }
}
