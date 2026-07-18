using Polly;
using ReswareConnectorWeb.RetryServices;
using ReswareConnectorWeb.Services;

namespace ReswareConnectorWeb.ReswareServices
{
    public class ServiceWrapperFactory : IServiceWrapperFactory, IDisposable
    {
        private readonly IServiceClientFactory _clientFactory;
        private readonly IRetryPolicyService _retryPolicyService;
        //private readonly List<IDisposable> _disposables = new();
        private readonly ILogger<IntegrationService> _logger;
        public ServiceWrapperFactory(
            IServiceClientFactory clientFactory,
            IRetryPolicyService retryPolicyService,
            ILogger<IntegrationService> logger)
        {   
            _clientFactory = clientFactory;
            _retryPolicyService = retryPolicyService;
            _logger = logger;
        }

        public IReceiveNoteServiceWrapper CreateReceiveNoteService()
        {
            var client = _clientFactory.CreateReceiveNoteServiceClient();

            var wrapper = new ReceiveNoteServiceWrapper(client, _logger);
            //_disposables.Add(wrapper);
            return wrapper;
        }

        public IReceiveActionEventServiceWrapper CreateReceiveActionEventService()
        {
            var client = _clientFactory.CreateReceiveActionEventServiceClient();

            var wrapper = new ReceiveActionEventServiceWrapper(client, _logger);
            //_disposables.Add(wrapper);
            return wrapper;
        }

        public IReceiveSearchDataServiceWrapper CreateReceiveSearchDataService()
        {
            var client = _clientFactory.CreateReceiveSearchDataServiceClient();

            var wrapper = new ReceiveSearchDataServiceWrapper(client, _logger);
            //_disposables.Add(wrapper);
            return wrapper;
        }

        public ICustomFieldServiceWrapper CreateCustomFieldService()
        {
            var wrapper = new CustomFieldServiceWrapper(_clientFactory, _retryPolicyService, _logger);
            //_disposables.Add(wrapper);
            return wrapper;
        }

        public void Dispose()
        {
            //foreach (var disposable in _disposables)
            //{
            //    disposable?.Dispose();
            //}
            //_disposables.Clear();
        }
    }
}
