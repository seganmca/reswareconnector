using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using ReswareConnectorWeb.Config;

namespace ReswareConnectorWeb.RetryServices
{
    public class RetryPolicyService : IRetryPolicyService
    {
        private readonly IAsyncPolicy _policy;
        private readonly IAsyncPolicy<object> _genericPolicy;
        private readonly ILogger<RetryPolicyService> _logger;

        public RetryPolicyService(IOptions<RetryPolicyConfig> config, ILogger<RetryPolicyService> logger)
        {
            _logger = logger;
            var settings = config.Value;

            // Timeout policy
            var timeoutPolicy = Policy.TimeoutAsync(
                TimeSpan.FromSeconds(settings.TimeoutSeconds),
                TimeoutStrategy.Pessimistic);

            //// Retry policy
            //var retryPolicy = Policy
            //    .Handle<Exception>()
            //    .WaitAndRetryAsync(
            //        settings.Retry.MaxRetryAttempts,
            //        retryAttempt => settings.Retry.RetryDelay,
            //        onRetry: (outcome, timespan, retryCount, context) =>
            //        {
            //            _logger.LogWarning("Retry {RetryCount} after {Delay} for operation", retryCount, timespan);
            //        });

            // Circuit breaker policy (if configured)
            IAsyncPolicy circuitBreakerPolicy = Policy.NoOpAsync();
            if (settings.CircuitBreaker != null)
            {
                circuitBreakerPolicy = Policy
                    .Handle<Exception>()
                    .AdvancedCircuitBreakerAsync(
                        failureThreshold: settings.CircuitBreaker.FailureRatio,
                        samplingDuration: TimeSpan.FromSeconds(settings.CircuitBreaker.SamplingDurationSeconds),
                        minimumThroughput: settings.CircuitBreaker.MinimumThroughput,
                        durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreaker.BreakDurationSeconds),
                        onBreak: (exception, timespan) =>
                        {
                            _logger.LogError(exception, "Circuit breaker opened for {Duration}", timespan);
                        },
                        onReset: () =>
                        {
                            _logger.LogInformation("Circuit breaker reset");
                        });
            }

            // Combine policies: Circuit Breaker -> Timeout -> Retry
            //_policy = Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy, retryPolicy);
            _policy = Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy);

            // Create generic policy
            _genericPolicy = CreateGenericPolicy(settings);
        }

        public IAsyncPolicy GetPolicy()
        {
            return _policy;
        }

        public IAsyncPolicy<T> GetPolicy<T>()
        {
            // Cast the generic policy to the specific type
            return _genericPolicy as IAsyncPolicy<T> ?? CreateSpecificGenericPolicy<T>();
        }

        private IAsyncPolicy<object> CreateGenericPolicy(RetryPolicyConfig settings)
        {
            var timeoutPolicy = Policy.TimeoutAsync<object>(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            //var retryPolicy = Policy<object>
            //    .Handle<Exception>()
            //    .WaitAndRetryAsync(
            //        settings.Retry.MaxRetryAttempts,
            //        retryAttempt => settings.Retry.RetryDelay,
            //        onRetry: (outcome, timespan, retryCount, context) =>
            //        {
            //            _logger.LogWarning("Retry {RetryCount} after {Delay} for generic operation", retryCount, timespan);
            //        });

            var circuitBreakerPolicy = Policy<object>
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: settings.CircuitBreaker?.FailureRatio ?? 0.1,
                    samplingDuration: TimeSpan.FromSeconds(settings.CircuitBreaker?.SamplingDurationSeconds ?? 30),
                    minimumThroughput: settings.CircuitBreaker?.MinimumThroughput ?? 3,
                    durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreaker?.BreakDurationSeconds ?? 5),
                    onBreak: (exception, timespan) =>
                    {
                        //_logger.LogError(exception, "Circuit breaker opened for {Duration} for generic operation", timespan);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset for generic operation");
                    });

            //return Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy, retryPolicy);
            return Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy);
        }

        private IAsyncPolicy<T> CreateSpecificGenericPolicy<T>()
        {
            var settings = new RetryPolicyConfig(); // Use defaults or inject config if needed

            var timeoutPolicy = Policy.TimeoutAsync<T>(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            //var retryPolicy = Policy<T>
            //    .Handle<Exception>()
            //    .WaitAndRetryAsync(
            //        settings.Retry.MaxRetryAttempts,
            //        retryAttempt => settings.Retry.RetryDelay,
            //        onRetry: (outcome, timespan, retryCount, context) =>
            //        {
            //            _logger.LogWarning("Retry {RetryCount} after {Delay} for operation with type {Type}",
            //                retryCount, timespan, typeof(T).Name);
            //        });

            var circuitBreakerPolicy = Policy<T>
                .Handle<Exception>()
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: settings.CircuitBreaker?.FailureRatio ?? 0.1,
                    samplingDuration: TimeSpan.FromSeconds(settings.CircuitBreaker?.SamplingDurationSeconds ?? 30),
                    minimumThroughput: settings.CircuitBreaker?.MinimumThroughput ?? 3,
                    durationOfBreak: TimeSpan.FromSeconds(settings.CircuitBreaker?.BreakDurationSeconds ?? 5),
                    onBreak: (exception, timespan) =>
                    {
                        //_logger.LogError(exception, "Circuit breaker opened for {Duration} for operation with type {Type}", timespan, typeof(T).Name);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset for operation with type {Type}", typeof(T).Name);
                    });

            //return Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy, retryPolicy);
            return Policy.WrapAsync(circuitBreakerPolicy, timeoutPolicy);
        }
    }
}
