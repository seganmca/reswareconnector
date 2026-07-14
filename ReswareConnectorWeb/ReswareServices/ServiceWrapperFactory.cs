using Polly;
using ReswareConnectorWeb.RetryServices;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ServiceWrapperFactory : IServiceWrapperFactory, IDisposable
    {
        private readonly IServiceClientFactory _clientFactory;
        private readonly IRetryPolicyService _retryPolicyService;
        private readonly List<IDisposable> _disposables = new();

        public ServiceWrapperFactory(
            IServiceClientFactory clientFactory,
            IRetryPolicyService retryPolicyService)
        {
            _clientFactory = clientFactory;
            _retryPolicyService = retryPolicyService;
        }

        public IReceiveNoteServiceWrapper CreateReceiveNoteService()
        {
            var wrapper = new ReceiveNoteServiceWrapper(_clientFactory, _retryPolicyService);
            _disposables.Add(wrapper);
            return wrapper;
        }

        public IReceiveActionEventServiceWrapper CreateReceiveActionEventService()
        {
            var wrapper = new ReceiveActionEventServiceWrapper(_clientFactory, _retryPolicyService);
            _disposables.Add(wrapper);
            return wrapper;
        }

        public IReceiveSearchDataServiceWrapper CreateReceiveSearchDataService()
        {
            var wrapper = new ReceiveSearchDataServiceWrapper(_clientFactory, _retryPolicyService);
            _disposables.Add(wrapper);
            return wrapper;
        }

        public ICustomFieldServiceWrapper CreateCustomFieldService()
        {
            var wrapper = new CustomFieldServiceWrapper(_clientFactory, _retryPolicyService);
            _disposables.Add(wrapper);
            return wrapper;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
    }
}
