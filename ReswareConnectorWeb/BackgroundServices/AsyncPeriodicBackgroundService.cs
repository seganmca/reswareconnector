using Microsoft.Extensions.Options;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.RetryServices;

namespace ReswareConnectorWeb.BackgroundServices
{
    public class AsyncPeriodicBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AsyncPeriodicBackgroundService> _logger;
        private readonly BackgroundServiceConfig _config;
        private readonly BackgroundServiceHealth<AsyncPeriodicBackgroundService> _serviceHealth;

        public AsyncPeriodicBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<AsyncPeriodicBackgroundService> logger,
            IOptions<BackgroundServiceConfig> config,
            BackgroundServiceHealth<AsyncPeriodicBackgroundService> serviceHealth)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _config = config.Value;
            _serviceHealth = serviceHealth;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_config.Enabled)
            {
                return;
            }
            _logger.LogInformation("Async Periodic Background Service is starting.");
            _serviceHealth.ReportSuccess();
            // Wait a bit before starting to allow the application to fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            using var timer = new PeriodicTimer(_config.SleepInterval);

            try
            {
                do
                {
                    try
                    {
                        _logger.LogInformation("Starting periodic background tasks execution");
                        
                        using var scope = _serviceScopeFactory.CreateScope();
                        var taskService = scope.ServiceProvider.GetRequiredService<IBackgroundTaskService>();

                        // Execute all tasks sequentially
                        await ExecuteTaskSafelyAsync(() => taskService.ReProcessFailedTransactionsAsync(_config, stoppingToken), "PerformReceiveNoteData");
                        //await ExecuteTaskSafelyAsync(() => taskService.PerformTitleHubUpdateAsync(_config, stoppingToken), "PerformTitleHubUpdate");
                        await ExecuteTaskSafelyAsync(() => taskService.ApplyRetentionPolicyAsync(_config, stoppingToken), "ApplyRetentionPolicy");

                        _logger.LogInformation("Completed periodic background tasks execution");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during periodic background tasks execution");
                        _serviceHealth.ReportFailure(ex);
                    }
                    _serviceHealth.ReportSuccess();
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation("Async Periodic Background Service is stopping due to cancellation.");
                _serviceHealth.ReportFailure(ex);
            }
        }

        private async Task ExecuteTaskSafelyAsync(Func<Task> task, string taskName)
        {
            try
            {
                _logger.LogDebug("Starting {TaskName}", taskName);
                await task();
                _logger.LogDebug("Completed {TaskName}", taskName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing {TaskName}", taskName);
                // Continue with other tasks even if one fails
            }
        }
    }
}
