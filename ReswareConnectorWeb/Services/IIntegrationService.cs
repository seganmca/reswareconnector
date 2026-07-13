using ActionEventServiceNS;
using Microsoft.AspNetCore.Mvc;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Enums;
using ReswareConnectorWeb.Models;
using SearchDataServiceNS;

namespace ReswareConnectorWeb.Services
{
    public interface IIntegrationService
    {
        Task<OrderResponseDto> PostOrderAsync(OrderDto request);
        Task ReProcessFailedTransactionsAsync(BackgroundServiceConfig config, CancellationToken cancellationToken);
        Task<ReceiveActionEventResponseDto> ReceiveActionEventAsync(ReceiveActionEventData request);
        //Task SendTransactionStatusUpdatesAsync(BackgroundServiceConfig config, CancellationToken cancellationToken);
        Task<object> GetTransactionDataAsync(long transactionReferenceNumber);
        Task<TransactionHistory> GetTransactionHistoryByFileNumber(string fileNumber);
        Task CleanupTransactionDataAsync(BackgroundServiceConfig config, CancellationToken cancellationToken);

        Task<byte[]> DownloadRequestAsync(string fileNumber);
        Task<byte[]> DownloadResponseAsync(string fileNumber, TransactionTypeEnum documentType);
    }
}
