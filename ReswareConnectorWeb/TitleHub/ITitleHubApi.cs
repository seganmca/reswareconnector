using Refit;
using ReswareConnectorWeb.Models;

namespace ReswareConnectorWeb.TitleHub
{
    public interface ITitleHubApi
    {
        [Post("/api/reswaretransaction/status")]
        Task PostTransactionStatus([Body] ReswareTransactionUpdateMessage transactionUpdate);

        [Post("/api/notification")]
        Task PostNotification([Body] Notification notification);
    }
}
