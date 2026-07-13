using ReswareConnectorWeb.Config;

namespace ReswareConnectorWeb.BackgroundServices
{
    public interface IBackgroundTaskService
    {
        Task ReProcessFailedTransactionsAsync(BackgroundServiceConfig config, CancellationToken cancellationToken = default);
        //Task PerformTitleHubUpdateAsync(BackgroundServiceConfig config, CancellationToken cancellationToken = default);
        Task ApplyRetentionPolicyAsync(BackgroundServiceConfig config, CancellationToken cancellationToken = default);
    }
}
