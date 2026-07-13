using Microsoft.Extensions.Options;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Services;

namespace ReswareConnectorWeb.BackgroundServices
{
    public class BackgroundTaskService : IBackgroundTaskService
    {
        private readonly ILogger<BackgroundTaskService> _logger;
        private readonly BackgroundServiceConfig _config;
        private readonly IIntegrationService _integrationService;

        public BackgroundTaskService(
            IIntegrationService integrationService,
            ILogger<BackgroundTaskService> logger,
            IOptions<BackgroundServiceConfig> config)
        {
            _integrationService = integrationService;
            _logger = logger;
            _config = config.Value;
        }

        public async Task ReProcessFailedTransactionsAsync(
            BackgroundServiceConfig config,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting PerformReceiveNoteData background task");

            await _integrationService.ReProcessFailedTransactionsAsync(_config, cancellationToken);

            await Task.Delay(1000, cancellationToken); // Simulate work
            _logger.LogInformation("Completed PerformReceiveNoteData background task");
        }
        /*
        public async Task PerformTitleHubUpdateAsync(
            BackgroundServiceConfig config,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting PerformTitleHubUpdate background task");

            await _integrationService.SendTransactionStatusUpdatesAsync(_config, cancellationToken);

            await Task.Delay(1000, cancellationToken); // Simulate work
            _logger.LogInformation("Completed PerformTitleHubUpdate background task");
        }
        */
        public async Task ApplyRetentionPolicyAsync(
            BackgroundServiceConfig config,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting ApplyRetentionPolicy background task");
            
            await _integrationService.CleanupTransactionDataAsync(_config, cancellationToken);

            await Task.Delay(1000, cancellationToken); // Simulate work
            _logger.LogInformation("Completed ApplyRetentionPolicy background task");
        }
    }
}
