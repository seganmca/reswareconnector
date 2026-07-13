using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ReswareConnectorWeb.BackgroundServices
{
    public class BackgroundServiceHealthCheck<T> : IHealthCheck where T : BackgroundService
    {
        private readonly BackgroundServiceHealth<T> _serviceHealth;
        private readonly ILogger<BackgroundServiceHealthCheck<T>> _logger;

        public BackgroundServiceHealthCheck(
            BackgroundServiceHealth<T> serviceHealth,
            ILogger<BackgroundServiceHealthCheck<T>> logger)
        {
            _serviceHealth = serviceHealth;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var lastSuccess = _serviceHealth.LastSuccessfullyCompletedAt;
            var lastFailure = _serviceHealth.LastFailureAt;
            var timeSinceLastSuccess = DateTime.UtcNow - lastSuccess;
            var consecutiveFailures = _serviceHealth.ConsecutiveFailures;

            var data = new Dictionary<string, object>
            {
                ["LastSuccessfulRun"] = lastSuccess.ToString("O"),
                ["LastFailure"] = lastFailure.ToString("O"),
                ["TimeSinceLastSuccess"] = timeSinceLastSuccess.ToString(),
                ["ConsecutiveFailures"] = consecutiveFailures,
                ["LastErrorMessage"] = _serviceHealth.LastErrorMessage ?? "None",
                ["ServiceType"] = typeof(T).Name
            };

            // Service has never run successfully
            if (lastSuccess == DateTime.MinValue)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    "Background service has never completed successfully",
                    data: data));
            }

            // Too many consecutive failures
            if (consecutiveFailures >= 3)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Background service has {consecutiveFailures} consecutive failures. Last error: {_serviceHealth.LastErrorMessage}",
                    data: data));
            }

            // Recent failure but not too many
            if (_serviceHealth.HasRecentFailure && consecutiveFailures > 0)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Background service had a recent failure but is still trying. Failures: {consecutiveFailures}",
                    data: data));
            }

            // Success is too old
            if (timeSinceLastSuccess > TimeSpan.FromSeconds(90))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Background service hasn't completed successfully in {timeSinceLastSuccess.TotalSeconds:F0} seconds",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Background service is healthy. Last success: {timeSinceLastSuccess.TotalSeconds:F0}s ago",
                data));
        }
    }
}
