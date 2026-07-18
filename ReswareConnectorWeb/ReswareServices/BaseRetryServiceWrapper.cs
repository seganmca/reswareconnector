using Polly;
using ReswareConnectorWeb.RetryServices;
using ReswareConnectorWeb.Services;
using System.ServiceModel;
using System.Threading;

namespace ReswareConnectorWeb.ReswareServices
{
    public abstract class BaseRetryServiceWrapper<TClient> : IDisposable
    where TClient : class, ICommunicationObject, IDisposable
    {
        private bool _disposed = false;
        protected TClient _client;
        private readonly ILogger<IntegrationService> _logger;

        protected BaseRetryServiceWrapper(
            TClient client,
            ILogger<IntegrationService> logger)
        {
            _client = client;
            _logger = logger;
        }

        protected async Task<TResult> ExecuteWithRetryAsync<TResult>(
            Func<Task<TResult>> operation,
            Func<System.ServiceModel.Channels.IChannel, bool> channelValidator = null)
        {
            _logger.LogInformation("ExecuteWithRetryAsync was called");
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            return await operation();
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (_client != null)
                    {
                        if (_client.State == CommunicationState.Opened)
                            _client.Close();
                        else
                            _client.Abort();

                        _client.Dispose();
                    }
                }
                catch
                {
                    // Ignore disposal errors
                }
                _disposed = true;
            }
        }
    }
}