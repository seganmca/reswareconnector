using System.ServiceModel;
using Polly;
using ReswareConnectorWeb.RetryServices;
using ReswareConnectorWeb.Services;

namespace ReswareConnectorWeb.ReswareServices
{
    public abstract class BaseRetryServiceWrapper<TClient> : IDisposable
    where TClient : class, ICommunicationObject, IDisposable
    {
        private readonly IRetryPolicyService _retryPolicyService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed = false;
        protected TClient _client;
        private readonly Func<TClient> _clientFactory;

        protected BaseRetryServiceWrapper(
            Func<TClient> clientFactory,
            IRetryPolicyService retryPolicyService)
        {
            _clientFactory = clientFactory;
            _retryPolicyService = retryPolicyService;
            _client = _clientFactory();
        }

        protected async Task<TResult> ExecuteWithRetryAsync<TResult>(
            Func<Task<TResult>> operation,
            Func<System.ServiceModel.Channels.IChannel, bool> channelValidator = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            var policy = _retryPolicyService.GetPolicy<TResult>();

            return await policy.ExecuteAsync(async () =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    return await ExecuteWithChannelManagementAsync(operation, channelValidator);
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }

        private async Task<TResult> ExecuteWithChannelManagementAsync<TResult>(
            Func<Task<TResult>> operation,
            Func<System.ServiceModel.Channels.IChannel, bool> channelValidator)
        {
            try
            {
                // Validate channel if validator provided
                if (channelValidator != null && !channelValidator(_client as System.ServiceModel.Channels.IChannel))
                {
                    RecreateClient();
                }

                // Ensure channel is open
                if (_client.State != CommunicationState.Opened && _client.State != CommunicationState.Opening)
                {
                    await Task.Run(() => _client.Open());
                }

                return await operation();
            }
            catch (Exception) when (IsChannelFaulted())
            {
                RecreateClient();
                throw;
            }
        }

        protected void RecreateClient()
        {
            try
            {
                if (_client != null)
                {
                    try
                    {
                        if (_client.State == CommunicationState.Opened)
                            _client.Close();
                        else
                            _client.Abort();
                    }
                    catch
                    {
                        _client.Abort();
                    }
                    finally
                    {
                        _client.Dispose();
                    }
                }
            }
            finally
            {
                _client = _clientFactory();
            }
        }

        protected bool IsChannelFaulted()
        {
            try
            {
                return _client?.State == CommunicationState.Faulted;
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