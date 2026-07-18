namespace ReswareConnectorWeb.Config
{
    public class BackgroundServiceConfig
    {
        public int MaxRetryAttemptDays { get; set; } = -1;
        public int MaxRetryAttemptsPerTransaction { get; set; } = 5;
        public TimeSpan SleepInterval { get; set; } = TimeSpan.FromMinutes(5);
        public RetentionPolicyConfig RetentionPolicy { get; set; } = new();
        public bool Enabled { get; set; } = false;
    }
}
