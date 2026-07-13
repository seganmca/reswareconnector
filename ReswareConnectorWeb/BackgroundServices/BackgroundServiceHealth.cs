namespace ReswareConnectorWeb.BackgroundServices
{
    public class BackgroundServiceHealth<T> where T : BackgroundService
    {
        private DateTime _successfullyCompletedAt = DateTime.MinValue;
        private DateTime _lastFailureAt = DateTime.MinValue;
        private string _lastErrorMessage;
        private int _consecutiveFailures = 0;
        private readonly object _lock = new object();

        public DateTime LastSuccessfullyCompletedAt => _successfullyCompletedAt;
        public DateTime LastFailureAt => _lastFailureAt;
        public string LastErrorMessage => _lastErrorMessage;
        public int ConsecutiveFailures => _consecutiveFailures;
        public bool HasRecentFailure => _lastFailureAt > _successfullyCompletedAt;

        public void ReportSuccess()
        {
            lock (_lock)
            {
                _successfullyCompletedAt = DateTime.UtcNow;
                _consecutiveFailures = 0;
                _lastErrorMessage = null;
            }
        }

        public void ReportFailure(string errorMessage = null)
        {
            lock (_lock)
            {
                _lastFailureAt = DateTime.UtcNow;
                _lastErrorMessage = errorMessage;
                _consecutiveFailures++;
            }
        }

        public void ReportFailure(Exception exception)
        {
            ReportFailure(exception.Message);
        }
    }
}
