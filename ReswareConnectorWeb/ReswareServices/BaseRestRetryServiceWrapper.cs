using ReswareConnectorWeb.RetryServices;

namespace ReswareConnectorWeb.ReswareServices
{
    public abstract class BaseRestRetryServiceWrapper<TClient> : IDisposable
        where TClient : class, IDisposable
    {
        private readonly IRetryPolicyService _retryPolicyService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed = false;
        protected TClient _client;
        private readonly Func<TClient> _clientFactory;

        protected BaseRestRetryServiceWrapper(
            Func<TClient> clientFactory,
            IRetryPolicyService retryPolicyService)
        {
            _clientFactory = clientFactory;
            _retryPolicyService = retryPolicyService;
            _client = _clientFactory();
        }

        protected async Task<TResult> ExecuteWithRetryAsync<TResult>(
            Func<Task<TResult>> operation,
            Func<TClient, bool> clientValidator = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            var policy = _retryPolicyService.GetPolicy<TResult>();

            return await policy.ExecuteAsync(async () =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    return await ExecuteWithClientManagementAsync(operation, clientValidator);
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }

        private async Task<TResult> ExecuteWithClientManagementAsync<TResult>(
            Func<Task<TResult>> operation,
            Func<TClient, bool> clientValidator)
        {
            try
            {
                // Validate client if validator provided
                if (clientValidator != null && !clientValidator(_client))
                {
                    RecreateClient();
                }

                return await operation();
            }
            catch (Exception) when (IsClientInvalid())
            {
                RecreateClient();
                throw;
            }
        }

        protected virtual void RecreateClient()
        {
            try
            {
                _client?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            finally
            {
                _client = _clientFactory();
            }
        }

        protected virtual bool IsClientInvalid()
        {
            try
            {
                // Check if client is disposed or in a bad state
                return _client == null;
            }
            catch
            {
                return true;
            }
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                _semaphore?.Dispose();
                try
                {
                    _client?.Dispose();
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
