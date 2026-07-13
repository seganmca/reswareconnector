using Polly;

namespace ReswareConnectorWeb.RetryServices
{
    public interface IRetryPolicyService
    {
        IAsyncPolicy GetPolicy();
        IAsyncPolicy<T> GetPolicy<T>();
    }
}
