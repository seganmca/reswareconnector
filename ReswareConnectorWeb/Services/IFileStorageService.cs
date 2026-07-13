using Microsoft.AspNetCore.Mvc;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Data.Entities;
using ReswareConnectorWeb.Enums;
using ReswareConnectorWeb.Models;
using SearchDataServiceNS;

namespace ReswareConnectorWeb.Services
{
    public interface IFileStorageService
    {
        //Task<string> StoreNoteRequestAsync(ReceiveNoteData requestData, List<IFormFile> files);
        //Task<ReceiveNoteData> RetrieveNoteRequestAsync(Transaction request, bool includeDocumentBody = true);

        Task<string> StoreTransactionDataAsync<T>(T requestData, TransactionTypeEnum transactionType);
        Task<string> StoreTransactionResponseDataAsync<T>(T responseData, string relativePath, TransactionTypeEnum transactionType);
        Task<T> RetrieveTransactionDataAsync<T>(Transaction request);

        Task<byte[]> GetDocumentFromTitleHub(string sourceFile);

        Task RemoveTransactionDataAsync(Transaction request);
        //Task CleanupTransactionDataAsync(DateOnly cutoffDate);

        string? GetRequestData(string dataPath);
        string? GetResponseData(string dataPath, TransactionTypeEnum documentType);

        Task StoreDataAsync<T>(string fileNumber, long trxnItemId, T requestData, TransactionTypeEnum transactionType, bool isRequest);
    }
}
