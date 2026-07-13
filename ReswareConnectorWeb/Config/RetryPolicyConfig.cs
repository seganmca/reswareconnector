namespace ReswareConnectorWeb.Config
{
    public class RetryPolicyConfig
    {
        public int TimeoutSeconds { get; set; } = 30;
        public RetrySettings Retry { get; set; } = new();
        public CircuitBreakerSettings? CircuitBreaker { get; set; }
    }

    public class RetrySettings
    {
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    }

    public class CircuitBreakerSettings
    {
        public int SamplingDurationSeconds { get; set; } = 30;
        public double FailureRatio { get; set; } = 0.1;
        public int MinimumThroughput { get; set; } = 3;
        public int BreakDurationSeconds { get; set; } = 5;
    }
}
