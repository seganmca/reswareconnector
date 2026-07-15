using ActionEventServiceNS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32.SafeHandles;
using Mysqlx.Crud;
using OrderPlacementServiceNS;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Macs;
using ReceiveNoteServiceNS;
using Refit;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Data;
using ReswareConnectorWeb.Data.Entities;
using ReswareConnectorWeb.Enums;
using ReswareConnectorWeb.Extensions;
using ReswareConnectorWeb.Models;
using ReswareConnectorWeb.ReswareServices;
using ReswareConnectorWeb.TitleHub;
using SearchDataServiceNS;
using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Reflection.Metadata;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static IdentityModel.OidcConstants;

namespace ReswareConnectorWeb.Services
{
    public class IntegrationService : IIntegrationService
    {
        private readonly IServiceWrapperFactory _serviceWrapperFactory;
        private readonly IFileStorageService _fileStorageService;
        private readonly ReswareConnectorDbContext _dbContext;
        private readonly ILogger<FileStorageService> _logger;
        private readonly ITitleHubApi _titleHubApi;
        public IntegrationService(IServiceWrapperFactory serviceWrapperFactory, 
                    IFileStorageService fileStorageService, 
                    ReswareConnectorDbContext reswareConnectorDbContext,
                    ITitleHubApi titleHubApi,
                    ILogger<FileStorageService> logger)
        {
            _serviceWrapperFactory = serviceWrapperFactory;
            _fileStorageService = fileStorageService;
            _dbContext = reswareConnectorDbContext;
            _titleHubApi = titleHubApi;
            _logger = logger;
        }

        #region Post Order

        public async Task<OrderResponseDto> PostOrderAsync(OrderDto order)
        {
            Transaction transaction;
            TransactionItem? noteTrxnItem = null, searchTrxnItem = null, eventTrxnItem = null, customFieldsTrxnItem = null;
            List<TransactionResponse> trxnResponses = new List<TransactionResponse>();
            bool customFieldsUpdated = true ;
            FileCustomFields? customFields = null;

           OrderResponseDto orderResponse = new OrderResponseDto();
            orderResponse.FileNumber = (order.SendNoteData ? order.NoteData?.FileNumber : (order.SendSearchData ? order.SearchData?.FileNumber : order.ActionEventData?.FileNumber)) ?? "";
            try
            {
                var storageLocation = await _fileStorageService.StoreTransactionDataAsync(order, TransactionTypeEnum.Order);

                transaction = new Transaction()
                {
                    FileNumber = orderResponse.FileNumber,
                    TransactionTypeId = (byte)TransactionTypeEnum.Order,
                    DataPath = storageLocation,
                    ReceivedTime = DateTime.Now
                };
                if (order.SendNoteData)
                {
                    noteTrxnItem = new TransactionItem
                    {
                        TransactionTypeId = (byte)TransactionTypeEnum.NoteDocument,
                        Processed = false,
                    };
                    transaction.Items.Add(noteTrxnItem);
                }
                if (order.SendSearchData)
                {
                    searchTrxnItem = new TransactionItem
                    {
                        TransactionTypeId = (byte)TransactionTypeEnum.SearchData,
                        Processed = false,
                    };
                    transaction.Items.Add(searchTrxnItem);

                    if (order.SearchData != null)
                    {
                        customFields = ExtractCustomFields(order.SearchData);
                        if (customFields != null && customFields.CustomFields.Any())
                        {
                            if (order.FileID <= 0)
                            {
                                throw new ArgumentException($"Invalid FileID {order.FileID}, CustomField API Requires Valid FileID");
                            }
                            customFieldsTrxnItem = new TransactionItem
                            {
                                TransactionTypeId = (byte)TransactionTypeEnum.CustomFields,
                                Processed = false,
                            };
                            transaction.Items.Add(customFieldsTrxnItem);
                        }
                    }
                }
                if (order.SendActionEventData)
                {
                    eventTrxnItem = new TransactionItem
                    {
                        TransactionTypeId = (byte)TransactionTypeEnum.ActionEvent,
                        Processed = false,
                    };
                    transaction.Items.Add(eventTrxnItem);
                }
                _dbContext.Transactions.Add(transaction);
                await _dbContext.SaveChangesAsync();
                orderResponse.TransactionReferenceNumber = transaction.TransactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store order request for {FileNumber}. Error: {Message}", orderResponse.FileNumber, ex.Message);
                throw;
            }

            if (customFieldsTrxnItem != null)
            {
                try
                {
                    customFieldsUpdated = false;
                    using var serviceWrapper = _serviceWrapperFactory.CreateCustomFieldService();
                    customFieldsUpdated = await SendCustomFieldsAsync(serviceWrapper, order, customFieldsTrxnItem.TransactionItemId, customFields!);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update customfields for file id {FileID}. Error: {Message}", order.FileID, ex.Message);
                }
                var trxnResponse = new TransactionResponse
                {
                    TransactionItemId = customFieldsTrxnItem.TransactionItemId,
                    ResponseCode = 0,
                    ResponseMessage = "CustomFields Updated",
                    ReceivedTime = DateTime.Now
                };
                trxnResponses.Add(trxnResponse);
                orderResponse.CustomFieldsUpdated = customFieldsUpdated;
                customFieldsTrxnItem.Processed = customFieldsUpdated;
            }

            if (customFieldsUpdated && order.SendNoteData && noteTrxnItem != null)
            {
                using var serviceWrapper = _serviceWrapperFactory.CreateReceiveNoteService();
                orderResponse.NoteDataResponse = await SendNoteDataAsync(serviceWrapper, order, noteTrxnItem.TransactionItemId);
                try
                {
                    await _fileStorageService.StoreTransactionResponseDataAsync(orderResponse.NoteDataResponse.OriginalResponse, transaction.DataPath, TransactionTypeEnum.NoteDocument);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store note response data for {FileNumber} transaction Id {TransactionId}. Error: {Message}", orderResponse.FileNumber, transaction.TransactionId, ex.Message);
                }
                var trxnResponse = new TransactionResponse
                {
                    TransactionItemId = noteTrxnItem.TransactionItemId,
                    ResponseCode = orderResponse.NoteDataResponse.ResponseCode,
                    ResponseMessage = orderResponse.NoteDataResponse.Message,
                    ReceivedTime = DateTime.Now
                };
                trxnResponses.Add(trxnResponse);
                orderResponse.NoteDataResponse.TransactionReferenceNumber = transaction.TransactionId;
                noteTrxnItem.Processed = !orderResponse.NoteDataResponse.AwaitDeferredResponse;
            }
            else orderResponse.NoteDataResponse = null;

            if (customFieldsUpdated && order.SendSearchData && searchTrxnItem != null)
            {
                using var serviceWrapper = _serviceWrapperFactory.CreateReceiveSearchDataService();
                orderResponse.SearchDataResponse = await SendSearchDataAsync(serviceWrapper, order, searchTrxnItem.TransactionItemId);
                try
                {
                    await _fileStorageService.StoreTransactionResponseDataAsync(orderResponse.SearchDataResponse.OriginalResponse, transaction.DataPath, TransactionTypeEnum.SearchData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store searchdata response data for {FileNumber} transaction Id {TransactionId}. Error: {Message}", orderResponse.FileNumber, transaction.TransactionId, ex.Message);
                }
                var trxnResponse = new TransactionResponse
                {
                    TransactionItemId = searchTrxnItem.TransactionItemId,
                    ResponseCode = orderResponse.SearchDataResponse.ResponseCode,
                    ResponseMessage = orderResponse.SearchDataResponse.Message,
                    ReceivedTime = DateTime.Now
                };
                trxnResponses.Add(trxnResponse);
                orderResponse.SearchDataResponse.TransactionReferenceNumber = transaction.TransactionId;
                searchTrxnItem.Processed = !orderResponse.SearchDataResponse.AwaitDeferredResponse;
            }
            else orderResponse.SearchDataResponse = null;

            if (customFieldsUpdated && order.SendActionEventData && eventTrxnItem != null && order.ActionEventData != null)
            {
                using var serviceWrapper = _serviceWrapperFactory.CreateReceiveActionEventService();
                orderResponse.ActionEventResponse = await SendActionEventAsync(serviceWrapper, order.ActionEventData, eventTrxnItem.TransactionItemId);
                try
                {
                    await _fileStorageService.StoreTransactionResponseDataAsync(orderResponse.ActionEventResponse.OriginalResponse, transaction.DataPath, TransactionTypeEnum.ActionEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store actionevent response data for {FileNumber} transaction Id {TransactionId}. Error: {Message}", orderResponse.FileNumber, transaction.TransactionId, ex.Message);
                }
                var trxnResponse = new TransactionResponse
                {
                    TransactionItemId = eventTrxnItem.TransactionItemId,
                    ResponseCode = orderResponse.ActionEventResponse.ResponseCode,
                    ResponseMessage = orderResponse.ActionEventResponse.Message,
                    ReceivedTime = DateTime.Now
                };
                trxnResponses.Add(trxnResponse);
                orderResponse.ActionEventResponse.TransactionReferenceNumber = transaction.TransactionId;
                eventTrxnItem.Processed = !orderResponse.ActionEventResponse.AwaitDeferredResponse;
            }
            else orderResponse.ActionEventResponse = null;

            try
            {
                transaction.Processed = (customFieldsTrxnItem == null || (customFieldsTrxnItem != null && customFieldsTrxnItem.Processed))
                                    && (noteTrxnItem == null || (noteTrxnItem != null && noteTrxnItem.Processed))
                                    && (searchTrxnItem == null || (searchTrxnItem != null && searchTrxnItem.Processed))
                                    && (eventTrxnItem == null || (eventTrxnItem != null && eventTrxnItem.Processed));

                if (customFieldsTrxnItem != null)
                {
                    customFieldsTrxnItem.ResponseSent = true;
                    customFieldsTrxnItem.LastUpdatedTime = DateTime.Now;
                }
                if (noteTrxnItem != null)
                {
                    noteTrxnItem.ResponseSent = true;
                    noteTrxnItem.LastUpdatedTime = DateTime.Now;
                }
                if (searchTrxnItem != null)
                {
                    searchTrxnItem.ResponseSent = true;
                    searchTrxnItem.LastUpdatedTime = DateTime.Now;
                }
                if (eventTrxnItem != null)
                {
                    eventTrxnItem.ResponseSent = true;
                    eventTrxnItem.LastUpdatedTime = DateTime.Now;
                }
                _dbContext.TransactionResponses.AddRange(trxnResponses);
                
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save response status to table for {FileNumber} transaction Id {TransactionId}. Error: {Message}", orderResponse.FileNumber, transaction.TransactionId, ex.Message);
            }
            return orderResponse;
        }

        private FileCustomFields ExtractCustomFields(ReceiveSearchDataDataDto searchData)
        {
            var customFields = new FileCustomFields { CustomFields = new List<CustomField>() };
            if (searchData != null)
            {
                //Extract custom fields from Easement Data
                var easementsWithCustomData = searchData.Easements?.Where(d => d.DocumentTypeID == 1443);
                if (easementsWithCustomData != null && easementsWithCustomData.Any())
                {
                    var updatedEasements = searchData.Easements.ToList();
                    foreach (var document in easementsWithCustomData)
                    {
                        var customField = new CustomField
                        {
                            Name = $"{document.EasementTypeName}",
                            Value = $"{document.Language}",
                        };
                        customFields.CustomFields.Add(customField);
                        updatedEasements.Remove(document);
                    }
                    searchData.Easements = updatedEasements.ToArray();
                }
                //Extract custom fields from Lien Data
                var liensWithCustomData = searchData.Liens?.Where(d => d.DocumentTypeID == 1443);
                if (liensWithCustomData != null && liensWithCustomData.Any())
                {
                    var updatedLiens = searchData.Liens.ToList();
                    foreach (var document in liensWithCustomData)
                    {
                        var customField = new CustomField
                        {
                            Name = $"{document.LienTypeName}",
                            Value = $"{document.Language}",
                        };
                        customFields.CustomFields.Add(customField);
                        updatedLiens.Remove(document);
                    }
                    searchData.Liens = updatedLiens.ToArray();
                }
            }
            return customFields;
        }

        private async Task<bool> SendCustomFieldsAsync(ICustomFieldServiceWrapper serviceWrapper, OrderDto order, long trxnItemId, FileCustomFields? customFields = null)
        {
            bool customFieldsUpdated = false;
            try
            {
                if (order != null)
                {
                    if (customFields == null && order.SearchData != null)
                    {
                        customFields = ExtractCustomFields(order.SearchData);
                    }
                    if (customFields != null && customFields.CustomFields.Any())
                    {
                        customFieldsUpdated = await serviceWrapper.UpdateCustomFieldsAsync(order.FileID, customFields);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending custom fields for FileID : {FileID}. Error: {Message}", order.FileID, ex.Message);
            }
            return customFieldsUpdated;
        }

        private async Task<ReceiveNoteResponseDto> SendNoteDataAsync(IReceiveNoteServiceWrapper serviceWrapper, OrderDto order, long trxnItemId)
        {
            ReceiveNoteResponseDto noteResponse = new();
            try
            {
                var requestDto = JsonSerializer.Serialize(order.NoteData);
                var request = requestDto.FromJson<ReceiveNoteData>();

                var fileNumber = order.NoteData?.FileNumber ?? "UnknownFileNumber";
                await _fileStorageService.StoreDataAsync(fileNumber, trxnItemId, request, TransactionTypeEnum.NoteDocument, true);

                //Load the documents from the titlehub storage
                if (request.Documents != null && request.Documents.Any())
                {
                    foreach (var note in request.Documents)
                    {
                        var sourceDoc = order.NoteData?.Documents?.FirstOrDefault(s => s.FileName == note.FileName);
                        if (sourceDoc != null && !string.IsNullOrEmpty(sourceDoc.SourceFile))
                        {
                            note.DocumentBody = await _fileStorageService.GetDocumentFromTitleHub(sourceDoc.SourceFile);
                        }
                    }
                }

                ReceiveNoteResponse response = await serviceWrapper.ReceiveNoteAsync(request);

                await _fileStorageService.StoreDataAsync(fileNumber, trxnItemId, response, TransactionTypeEnum.NoteDocument, false);

                noteResponse = new ReceiveNoteResponseDto
                {
                    OriginalResponse = response,
                    Message = response.Message,
                    ResponseCodeName = response.ResponseCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending note request for {FileNumber}. Error: {Message}", order.NoteData.FileNumber, ex.Message);
                noteResponse = new ReceiveNoteResponseDto
                {
                    Message = ex.Message,
                    ResponseCodeName = ReceiveNoteResponseCode.UNEXPECTED_ERROR
                };
            }
            noteResponse.FileNumber = order.NoteData.FileNumber;
            noteResponse.AwaitDeferredResponse = noteResponse.ResponseCodeName == ReceiveNoteResponseCode.UNEXPECTED_ERROR;
            return noteResponse;
        }

        private async Task<ReceiveSearchDataResponseDto> SendSearchDataAsync(IReceiveSearchDataServiceWrapper serviceWrapper, OrderDto order, long trxnItemId)
        {
            ReceiveSearchDataResponseDto searchResponse = new();
            try
            {
                var fileNumber = order.SearchData?.FileNumber ?? "UnknownFileNumber";
                await _fileStorageService.StoreDataAsync(fileNumber, trxnItemId, order.SearchData, TransactionTypeEnum.SearchData, true);

                var searchData = SearchDataMapper.MapToReceiveSearchDataData(order.SearchData);

                ReceiveSearchDataResponse response = await serviceWrapper.ReceiveSearchDataAsync(searchData);

                await _fileStorageService.StoreDataAsync(fileNumber, trxnItemId, response, TransactionTypeEnum.SearchData, false);

                searchResponse = new ReceiveSearchDataResponseDto
                {
                    OriginalResponse = response,
                    Message = response.Message,
                    ResponseCodeName = response.ResponseCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending searchdata request for {FileNumber}. Error: {Message}", order.SearchData.FileNumber, ex.Message);
                searchResponse = new ReceiveSearchDataResponseDto
                {
                    Message = ex.Message,
                    ResponseCodeName = ReceiveSearchDataResponseCode.UNEXPECTED_ERROR
                };
            }
            searchResponse.FileNumber = order.SearchData.FileNumber;
            searchResponse.AwaitDeferredResponse = searchResponse.ResponseCodeName == ReceiveSearchDataResponseCode.UNEXPECTED_ERROR;
            return searchResponse;
        }

        private async Task<ReceiveActionEventResponseDto> SendActionEventAsync(IReceiveActionEventServiceWrapper serviceWrapper, ReceiveActionEventData request, long trxnItemId)
        {
            ReceiveActionEventResponseDto eventResponse = new();
            try
            {
                var fileNumber = string.IsNullOrWhiteSpace(request.FileNumber) ? "UnknownFileNumber" : request.FileNumber;
                await _fileStorageService.StoreDataAsync(fileNumber, trxnItemId, request, TransactionTypeEnum.ActionEvent, true);

                ReceiveActionEventResponse response = await serviceWrapper.ReceiveActionEventAsync(request);

                await _fileStorageService.StoreDataAsync(fileNumber, trxnItemId, response, TransactionTypeEnum.ActionEvent, false);

                eventResponse = new ReceiveActionEventResponseDto
                {
                    OriginalResponse = response,
                    Message = response.Message,
                    ResponseCodeName = response.ResponseCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending actionevent request for {FileNumber}. Error: {Message}", request.FileNumber, ex.Message);
                eventResponse = new ReceiveActionEventResponseDto
                {
                    Message = ex.Message,
                    ResponseCodeName = ReceiveActionEventResponseCode.UNEXPECTED_ERROR
                };
            }
            eventResponse.FileNumber = request.FileNumber;
            eventResponse.AwaitDeferredResponse = eventResponse.ResponseCodeName == ReceiveActionEventResponseCode.UNEXPECTED_ERROR;
            return eventResponse;
        }


        #endregion Post Order

        #region ReceiveNote

        /*
        public async Task<ReceiveNoteResponseDto> ReceiveNoteAsync(ReceiveNoteData request, List<IFormFile> files)
        {
            Transaction transaction;
            ReceiveNoteResponseDto noteResponse;
            try
            {
                var storageLocation = await _fileStorageService.StoreNoteRequestAsync(request, files);

                foreach (IFormFile file in files)
                {
                    var document = request.Documents.Where(d => string.Equals(d.FileName, file.FileName, StringComparison.OrdinalIgnoreCase) ||
                                                                string.Equals(d.FileName, file.Name, StringComparison.OrdinalIgnoreCase) ||
                                                                string.Equals(Path.GetFileNameWithoutExtension(d.FileName), Path.GetFileNameWithoutExtension(file.FileName), StringComparison.OrdinalIgnoreCase))
                                                    .FirstOrDefault();
                    if (document != null)
                    {
                        document.DocumentBody = await GetFileDataAsync(file);
                    }
                }

                transaction = new Transaction()
                {
                    FileNumber = request.FileNumber,
                    TransactionTypeId = (byte)TransactionTypeEnum.NoteDocument,
                    DataPath = storageLocation,
                    ReceivedTime = DateTime.Now
                };

                _dbContext.Transactions.Add(transaction);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store note request for {FileNumber}. Error: {Message}", request.FileNumber, ex.Message);
                throw;
            }
            try
            {
                using var serviceWrapper = _serviceWrapperFactory.CreateReceiveNoteService();
                ReceiveNoteResponse response = await serviceWrapper.ReceiveNoteAsync(request);

                noteResponse = new ReceiveNoteResponseDto
                {
                    Message = response.Message,
                    TransactionReferenceNumber = transaction.TransactionId,
                    AwaitDeferredResponse = response.ResponseCode == ReceiveNoteResponseCode.UNEXPECTED_ERROR,
                    ResponseCodeName = response.ResponseCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending note request for {FileNumber}. Error: {Message}", request.FileNumber, ex.Message);
                noteResponse = new ReceiveNoteResponseDto
                {
                    Message = ex.Message,
                    TransactionReferenceNumber = transaction.TransactionId,
                    AwaitDeferredResponse = true,
                    ResponseCodeName = ReceiveNoteResponseCode.UNEXPECTED_ERROR
                };
            }

            try
            {
                transaction.ResponseCode = (int)noteResponse.ResponseCodeName;
                transaction.ResponseMessage = noteResponse.Message.Limit(500);
                transaction.LastUpdatedTime = DateTime.Now;
                transaction.ResponseSent = !noteResponse.AwaitDeferredResponse;
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending note request for {FileNumber}. Error: {Message}", request.FileNumber, ex.Message);
            }
            return noteResponse;
        }

        private async Task<byte[]> GetFileDataAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        */
        #endregion ReceiveNote

        #region ReceiveSearchData

        /*
        public async Task<ReceiveSearchDataResponseDto> ReceiveSearchDataAsync(ReceiveSearchDataData request)
        {
            Transaction transaction;
            ReceiveSearchDataResponseDto searchResponse;
            try
            {
                var storageLocation = await _fileStorageService.StoreTransactionDataAsync<ReceiveSearchDataData>(request, TransactionTypeEnum.SearchData);

                transaction = new Transaction()
                {
                    FileNumber = request.FileNumber,
                    TransactionTypeId = (byte)TransactionTypeEnum.SearchData,
                    DataPath = storageLocation,
                    ReceivedTime = DateTime.Now
                };

                _dbContext.Transactions.Add(transaction);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store searchdata request for {FileNumber}. Error: {Message}", request.FileNumber, ex.Message);
                throw;
            }
            try
            {
                using var serviceWrapper = _serviceWrapperFactory.CreateReceiveSearchDataService();
                ReceiveSearchDataResponse response = await serviceWrapper.ReceiveSearchDataAsync(request);

                searchResponse = new ReceiveSearchDataResponseDto
                {
                    Message = response.Message,
                    TransactionReferenceNumber = transaction.TransactionId,
                    AwaitDeferredResponse = response.ResponseCode == ReceiveSearchDataResponseCode.UNEXPECTED_ERROR,
                    ResponseCodeName = response.ResponseCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending searchdata request for {FileNumber}. Error: {Message}", request.FileNumber, ex.Message);
                searchResponse = new ReceiveSearchDataResponseDto
                {
                    Message = ex.Message,
                    TransactionReferenceNumber = transaction.TransactionId,
                    AwaitDeferredResponse = true,
                    ResponseCodeName = ReceiveSearchDataResponseCode.UNEXPECTED_ERROR
                };
            }

            transaction.ResponseCode = (int)searchResponse.ResponseCodeName;
            transaction.ResponseMessage = searchResponse.Message.Limit(500);
            transaction.LastUpdatedTime = DateTime.Now;
            transaction.ResponseSent = !searchResponse.AwaitDeferredResponse;
            await _dbContext.SaveChangesAsync();

            return searchResponse;

        }

        */

        #endregion ReceiveSearchData

        #region ReceiveActionEvent

        public async Task<ReceiveActionEventResponseDto> ReceiveActionEventAsync(ReceiveActionEventData request)
        {
            Transaction transaction;
            TransactionItem transactionItem = null;
            TransactionResponse trxnResponse = null;

            ReceiveActionEventResponseDto actionEventResponse;
            try
            {
                var storageLocation = await _fileStorageService.StoreTransactionDataAsync(request, TransactionTypeEnum.ActionEvent);

                transaction = new Transaction()
                {
                    FileNumber = request.FileNumber,
                    TransactionTypeId = (byte)TransactionTypeEnum.ActionEvent,
                    DataPath = storageLocation,
                    ReceivedTime = DateTime.Now
                };

                transactionItem = new TransactionItem
                {
                    TransactionTypeId = (byte)TransactionTypeEnum.ActionEvent,
                    Processed = false,
                    ResponseSent = false,
                };
                transaction.Items.Add(transactionItem);

                _dbContext.Transactions.Add(transaction);
                await _dbContext.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store action event request for {FileNumber}. Error: {Message}", request.FileNumber, ex.Message);
                throw;
            }

            using var serviceWrapper = _serviceWrapperFactory.CreateReceiveActionEventService();
            actionEventResponse = await SendActionEventAsync(serviceWrapper, request, transactionItem.TransactionItemId);
            actionEventResponse.TransactionReferenceNumber = transaction.TransactionId;
            actionEventResponse.AwaitDeferredResponse = actionEventResponse.ResponseCodeName == ReceiveActionEventResponseCode.UNEXPECTED_ERROR;
            transactionItem.Processed = !actionEventResponse.AwaitDeferredResponse;

            try
            {
                var txResponse = new TransactionResponse
                {
                    TransactionItemId = transactionItem.TransactionItemId,
                    ResponseCode = actionEventResponse.ResponseCode,
                    ResponseMessage = actionEventResponse.Message,
                    ReceivedTime = DateTime.Now
                };
                _dbContext.TransactionResponses.Add(txResponse);

                transaction.Processed = transactionItem.Processed;
                transactionItem.ResponseSent = true;
                try
                {
                    await _fileStorageService.StoreTransactionResponseDataAsync(actionEventResponse.OriginalResponse, transaction.DataPath, TransactionTypeEnum.ActionEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to store actionevent response data for {FileNumber} transaction Id {TransactionId}. Error: {Message}", request.FileNumber, transaction.TransactionId, ex.Message);
                }
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save response status to table for {FileNumber} transaction Id {TransactionId}. Error: {Message}", request.FileNumber, transaction.TransactionId, ex.Message);
            }
            return actionEventResponse;
        }

        #endregion ReceiveActionEvent

        #region Reprocess Failed Transactions

        public async Task ReProcessFailedTransactionsAsyncOld(BackgroundServiceConfig config, CancellationToken cancellationToken)
        {
            /*
            IEnumerable<Transaction> transactions;
            try
            {
                transactions = _dbContext.Transactions.Where(t =>
                                                        ((t.TransactionTypeId == (byte)TransactionTypeEnum.NoteDocument && t.ResponseCode == (int)ReceiveNoteResponseCode.UNEXPECTED_ERROR)
                                                           || (t.TransactionTypeId == (byte)TransactionTypeEnum.SearchData && t.ResponseCode == (int)ReceiveSearchDataResponseCode.UNEXPECTED_ERROR)
                                                           || (t.TransactionTypeId == (byte)TransactionTypeEnum.ActionEvent && t.ResponseCode == (int)ReceiveActionEventResponseCode.UNEXPECTED_ERROR))
                                                        && (t.RetryCount == null || t.RetryCount < config.MaxRetryAttemptsPerTransaction)
                                                        && (t.ResponseSent == null || t.ResponseSent == false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve transactions for reprocessing. Error: {Message}", ex.Message);
                return;
            }

            Lazy<IReceiveNoteServiceWrapper> noteServiceWrapper = new Lazy<IReceiveNoteServiceWrapper>(() => _serviceWrapperFactory.CreateReceiveNoteService());
            Lazy<IReceiveSearchDataServiceWrapper> searchServiceWrapper = new Lazy<IReceiveSearchDataServiceWrapper>(() => _serviceWrapperFactory.CreateReceiveSearchDataService());
            Lazy<IReceiveActionEventServiceWrapper> eventServiceWrapper = new Lazy<IReceiveActionEventServiceWrapper>(() => _serviceWrapperFactory.CreateReceiveActionEventService());

            try
            {
                foreach (var transaction in transactions)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    string? responseMessage = null;
                    int? responseCode = null;

                    if (transaction.TransactionTypeId == (byte)TransactionTypeEnum.NoteDocument)
                    {
                        try
                        {
                            var request = await _fileStorageService.RetrieveNoteRequestAsync(transaction);
                            var noteService = noteServiceWrapper.Value;
                            ReceiveNoteResponse response = await noteService.ReceiveNoteAsync(request);

                            responseCode = (int)response.ResponseCode;
                            responseMessage = response.Message;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed (re)sending note request for {transaction.FileNumber}, Transaction (Id:{transaction.TransactionId}) . Error: {ex.Message}");
                            responseCode = (int)ReceiveNoteResponseCode.UNEXPECTED_ERROR;
                            responseMessage = ex.Message;
                        }
                    }
                    else if (transaction.TransactionTypeId == (byte)TransactionTypeEnum.SearchData)
                    {
                        try
                        {
                            var request = await _fileStorageService.RetrieveTransactionDataAsync<ReceiveSearchDataData>(transaction);
                            var searchService = searchServiceWrapper.Value;
                            ReceiveSearchDataResponse response = await searchService.ReceiveSearchDataAsync(request);

                            responseCode = (int)response.ResponseCode;
                            responseMessage = response.Message;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed (re)sending searchdata request for {transaction.FileNumber}, Transaction (Id:{transaction.TransactionId}) . Error: {ex.Message}");
                            responseCode = (int)ReceiveSearchDataResponseCode.UNEXPECTED_ERROR;
                            responseMessage = ex.Message;
                        }
                    }
                    else if (transaction.TransactionTypeId == (byte)TransactionTypeEnum.ActionEvent)
                    {
                        try
                        {
                            var request = await _fileStorageService.RetrieveTransactionDataAsync<ReceiveActionEventData>(transaction);
                            var eventService = eventServiceWrapper.Value;
                            ReceiveActionEventResponse response = await eventService.ReceiveActionEventAsync(request);

                            responseCode = (int)response.ResponseCode;
                            responseMessage = response.Message;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed (re)sending actionevent request for {transaction.FileNumber}, Transaction (Id:{transaction.TransactionId}) . Error: {ex.Message}");
                            responseCode = (int)ReceiveActionEventResponseCode.UNEXPECTED_ERROR;
                            responseMessage = ex.Message;
                        }
                    }

                    try
                    {
                        transaction.RetryCount = (byte)((transaction.RetryCount ?? (byte)0) + 1);
                        transaction.ResponseCode = responseCode;
                        transaction.ResponseMessage = responseMessage?.Limit(500);
                        transaction.LastUpdatedTime = DateTime.Now;
                        transaction.ResponseSent = false;
                        await _dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to update Transaction (Id:{transaction.TransactionId}) for FileNumber {transaction.FileNumber}. Error: {ex.Message}");
                    }
                }
            }
            finally
            {
                if (noteServiceWrapper.IsValueCreated)
                    noteServiceWrapper.Value.Dispose();

                if (searchServiceWrapper.IsValueCreated)
                    searchServiceWrapper.Value.Dispose();

                if (eventServiceWrapper.IsValueCreated)
                    eventServiceWrapper.Value.Dispose();
            }
            */
        }

        public async Task ReProcessFailedTransactionsAsync(BackgroundServiceConfig config, CancellationToken cancellationToken)
        {
            IEnumerable<Transaction> transactions;
            try
            {
                var cutoffTime = DateTime.Now - config.SleepInterval;
                var query = _dbContext.Transactions
                    .Include(t => t.Items
                        .Where(i => !i.Processed && i.LastUpdatedTime < cutoffTime))
                    .Where(t => !t.Processed);

                // Apply ReceivedTime filter only if MaxRetryAttemptDays is not -1
                if (config.MaxRetryAttemptDays != -1)
                {
                    var cutoffReceivedTime = DateTime.Now.AddDays(-config.MaxRetryAttemptDays);
                    query = query.Where(t => t.ReceivedTime > cutoffReceivedTime);
                }

                transactions = await query.OrderBy(t => t.FileNumber).ThenBy(t=> t.TransactionTypeId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve transactions for reprocessing. Error: {Message}", ex.Message);
                return;
            }

            Lazy<ICustomFieldServiceWrapper> customFieldServiceWrapper = new Lazy<ICustomFieldServiceWrapper>(() => _serviceWrapperFactory.CreateCustomFieldService());
            Lazy<IReceiveNoteServiceWrapper> noteServiceWrapper = new Lazy<IReceiveNoteServiceWrapper>(() => _serviceWrapperFactory.CreateReceiveNoteService());
            Lazy<IReceiveSearchDataServiceWrapper> searchServiceWrapper = new Lazy<IReceiveSearchDataServiceWrapper>(() => _serviceWrapperFactory.CreateReceiveSearchDataService());
            Lazy<IReceiveActionEventServiceWrapper> eventServiceWrapper = new Lazy<IReceiveActionEventServiceWrapper>(() => _serviceWrapperFactory.CreateReceiveActionEventService());

            try
            {
                foreach (var transactionItem in transactions.SelectMany(t=>t.Items))
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    bool isProcessed = false;
                    bool isResponseSent = false;

                    if (transactionItem.TransactionTypeId == (byte)TransactionTypeEnum.CustomFields)
                    {
                        try
                        {
                            var request = await _fileStorageService.RetrieveTransactionDataAsync<OrderDto>(transactionItem.Transaction);
                            var response = await SendCustomFieldsAsync(customFieldServiceWrapper.Value, request, transactionItem.TransactionItemId);

                            var txResponse = new TransactionResponse
                            {
                                TransactionItemId = transactionItem.TransactionItemId,
                                ResponseCode = response ? 0 : -1,
                                ResponseMessage = response ? "CustomFields Updated" : "CustomFields Updated Failed",
                                ReceivedTime = DateTime.Now
                            };
                            _dbContext.TransactionResponses.Add(txResponse);
                            isResponseSent = await UpdateTransactionStatusAsync(transactionItem, TransactionTypeEnum.CustomFields, txResponse);
                            isProcessed = response;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while reprocessing CustomFields Update request for FileNumber: {transactionItem.Transaction.FileNumber}, TransactionID: {transactionItem.TransactionId}");
                        }
                    }
                    else if (transactionItem.TransactionTypeId == (byte)TransactionTypeEnum.NoteDocument)
                    {
                        try
                        {
                            var request = await _fileStorageService.RetrieveTransactionDataAsync<OrderDto>(transactionItem.Transaction);
                            var response = await SendNoteDataAsync(noteServiceWrapper.Value, request, transactionItem.TransactionItemId);

                            var txResponse = new TransactionResponse
                            {
                                TransactionItemId = transactionItem.TransactionItemId,
                                ResponseCode = response.ResponseCode,
                                ResponseMessage = response.Message,
                                ReceivedTime = DateTime.Now
                            };
                            _dbContext.TransactionResponses.Add(txResponse);
                            isResponseSent = await UpdateTransactionStatusAsync(transactionItem, TransactionTypeEnum.NoteDocument, txResponse);
                            isProcessed = response.ResponseCodeName != ReceiveNoteResponseCode.UNEXPECTED_ERROR;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while reprocessing NoteDocument request for FileNumber: {transactionItem.Transaction.FileNumber}, TransactionID: {transactionItem.TransactionId}");
                        }
                    }
                    else if (transactionItem.TransactionTypeId == (byte)TransactionTypeEnum.SearchData)
                    {
                        try
                        {
                            var request = await _fileStorageService.RetrieveTransactionDataAsync<OrderDto>(transactionItem.Transaction);
                            if (request != null && request.SearchData != null)
                            {
                                ExtractCustomFields(request.SearchData);//Updates the original Search data by removing CustomField related Liens and Easements
                            }
                            var response = await SendSearchDataAsync(searchServiceWrapper.Value, request, transactionItem.TransactionItemId);

                            var txResponse = new TransactionResponse
                            {
                                TransactionItemId = transactionItem.TransactionItemId,
                                ResponseCode = response.ResponseCode,
                                ResponseMessage = response.Message,
                                ReceivedTime = DateTime.Now
                            };
                            _dbContext.TransactionResponses.Add(txResponse);
                            isResponseSent = await UpdateTransactionStatusAsync(transactionItem, TransactionTypeEnum.SearchData, txResponse);
                            isProcessed = response.ResponseCodeName != ReceiveSearchDataResponseCode.UNEXPECTED_ERROR;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while reprocessing SearchData request for FileNumber: {transactionItem.Transaction.FileNumber}, TransactionID: {transactionItem.TransactionId}");
                        }
                    }
                    else if (transactionItem.TransactionTypeId == (byte)TransactionTypeEnum.ActionEvent)
                    {
                        try
                        {
                            var request = await _fileStorageService.RetrieveTransactionDataAsync<OrderDto>(transactionItem.Transaction);
                            var response = await SendActionEventAsync(eventServiceWrapper.Value, request.ActionEventData, transactionItem.TransactionItemId);

                            var txResponse = new TransactionResponse
                            {
                                TransactionItemId = transactionItem.TransactionItemId,
                                ResponseCode = (int)response.ResponseCode,
                                ResponseMessage = response.Message,
                                ReceivedTime = DateTime.Now
                            };
                            _dbContext.TransactionResponses.Add(txResponse);
                            isResponseSent = await UpdateTransactionStatusAsync(transactionItem, TransactionTypeEnum.ActionEvent, txResponse);
                            isProcessed = response.ResponseCodeName != ReceiveActionEventResponseCode.UNEXPECTED_ERROR;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed (re)sending actionevent request for {transactionItem.Transaction.FileNumber}, Transaction (Id:{transactionItem.TransactionId}) . Error: {ex.Message}");
                        }
                    }

                    try
                    {
                        transactionItem.RetryCount = (byte)((transactionItem.RetryCount ?? (byte)0) + 1);
                        transactionItem.Processed = isProcessed;
                        transactionItem.ResponseSent = isResponseSent;
                        transactionItem.LastUpdatedTime = DateTime.Now;
                        await _dbContext.SaveChangesAsync();

                        if(_dbContext.TransactionItems.Where(ti=> ti.TransactionId == transactionItem.TransactionId).All(ti=>ti.Processed))
                        {
                            transactionItem.Transaction.Processed = true;
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to update Transaction (Id:{transactionItem.TransactionId}) for FileNumber {transactionItem.Transaction.FileNumber}. Error: {ex.Message}");
                    }
                }
            }
            finally
            {
                if (customFieldServiceWrapper.IsValueCreated)
                    customFieldServiceWrapper.Value.Dispose();

                if (noteServiceWrapper.IsValueCreated)
                    noteServiceWrapper.Value.Dispose();

                if (searchServiceWrapper.IsValueCreated)
                    searchServiceWrapper.Value.Dispose();

                if (eventServiceWrapper.IsValueCreated)
                    eventServiceWrapper.Value.Dispose();
            }
        }

        private async Task<bool> UpdateTransactionStatusAsync(TransactionItem transactionItem, TransactionTypeEnum txType, TransactionResponse txResponse)
        {
            bool sent = false;
            try
            {
                var statusMsg = new ReswareTransactionUpdateMessage
                {
                    FileNumber = transactionItem.Transaction.FileNumber,
                    TransactionReferenceNumber = transactionItem.TransactionId,
                    TransactionType = (int)txType,
                    TransactionTypeName = txType,
                    ResponseCode = txResponse.ResponseCode,
                    Message = txResponse.ResponseMessage
                };
                await _titleHubApi.PostTransactionStatus(statusMsg);
                sent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to post transaction status to TitleHub (Transaction Id:{transactionItem.TransactionId},  FileNumber {transactionItem.Transaction.FileNumber}. Error: {ex.Message}");
            }
            return sent;
        }

        #endregion Reprocess Failed Transactions

        #region Send Transaction Status Update To TitleHub

        /*

        public async Task SendTransactionStatusUpdatesAsync(BackgroundServiceConfig config, CancellationToken cancellationToken)
        {

            try
            {
                if(cancellationToken.IsCancellationRequested) return;

                var transactions = _dbContext.Transactions.Where(t => t.ResponseSent == false
                                                            && (((t.TransactionTypeId == (byte)TransactionTypeEnum.NoteDocument && t.ResponseCode != (int)ReceiveNoteResponseCode.UNEXPECTED_ERROR)
                                                                || (t.TransactionTypeId == (byte)TransactionTypeEnum.SearchData && t.ResponseCode != (int)ReceiveSearchDataResponseCode.UNEXPECTED_ERROR)
                                                                || (t.TransactionTypeId == (byte)TransactionTypeEnum.ActionEvent && t.ResponseCode != (int)ReceiveActionEventResponseCode.UNEXPECTED_ERROR)
                                                                )
                                                               || t.RetryCount >= config.MaxRetryAttemptsPerTransaction));
                var responses = transactions.Select(t => new ReswareTransactionUpdateMessage
                {
                    TransactionType = t.TransactionTypeId,
                    TransactionTypeName = (TransactionTypeEnum)t.TransactionTypeId,
                    TransactionId = t.TransactionId,
                    FileNumber = t.FileNumber,
                    ResponseCode = t.ResponseCode,
                    Message = t.ResponseMessage
                }).ToList();
                if (responses.Any())
                {
                    await _titleHubApi.PostTransactionUpdate(responses);
                    foreach (var item in transactions)
                    {
                        item.ResponseSent = true;
                        item.LastUpdatedTime = DateTime.Now;
                    }
                    _dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending Transaction Update to TitleHub. Error: {Message}", ex.Message);
            }
        }
        */

        #endregion Send Transaction Status Update To TitleHub

        #region Transaction Info

        public async Task<object> GetTransactionDataAsync(long transactionReferenceNumber)
        {
            var transaction = await _dbContext.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionReferenceNumber);
            if (transaction != null)
            {
                switch ((TransactionTypeEnum)transaction.TransactionTypeId) 
                {
                    case TransactionTypeEnum.Order:
                        var orderData = await _fileStorageService.RetrieveTransactionDataAsync<OrderDto>(transaction);
                        return orderData;
                    case TransactionTypeEnum.ActionEvent:
                        var eventData = await _fileStorageService.RetrieveTransactionDataAsync<ReceiveActionEventData>(transaction);
                        return eventData;
                }
            }
            throw new ArgumentException($"Invalid TransactionReferenceNumber: {transactionReferenceNumber}");
        }

        public async Task<TransactionHistory> GetTransactionHistoryByFileNumber(string fileNumber)
        {
            var result = await _dbContext.Transactions
                .Where(t => t.FileNumber == fileNumber)
                .OrderByDescending(t => t.ReceivedTime)
                .Select(t => new
                {
                    Transaction = t,
                    Items = t.Items.OrderBy(i => i.TransactionItemId).ToList(),
                    Responses = t.Items.SelectMany(i => i.Responses).ToList()
                })
                .ToListAsync();

            var transactionDtos = result.Select(x => new TransactionDto
            {
                TransactionId = x.Transaction.TransactionId,
                TransactionType = x.Transaction.TransactionTypeId,
                TransactionTypeName = (TransactionTypeEnum)x.Transaction.TransactionTypeId,
                ReceivedTime = x.Transaction.ReceivedTime,
                IsCompleted = x.Transaction.Processed,
                Requests = x.Items.Select(item => new ReswareRequest
                {
                    TransactionType = item.TransactionTypeId,
                    TransactionTypeName = (TransactionTypeEnum)item.TransactionTypeId,
                    IsCompleted = item.Processed,
                    Data = LoadData(x.Transaction, (TransactionTypeEnum)item.TransactionTypeId),
                    ResponseHistory = x.Responses
                        .Where(r => r.TransactionItemId == item.TransactionItemId)
                        .Select(response => new ReswareResponse
                        {
                            Message = response.ResponseMessage ?? string.Empty,
                            ResponseCode = response.ResponseCode,
                            ReceivedTime = response.ReceivedTime
                        })
                        .OrderByDescending(r => r.ReceivedTime)
                        .ToList()
                }).ToList()
            }).ToList();

            return new TransactionHistory
            {
                FileNumber = fileNumber,
                Transactions = transactionDtos
            };
        }

        private object LoadData(Transaction transaction, TransactionTypeEnum transactionType)
        {
            switch (transactionType)
            {
                case TransactionTypeEnum.NoteDocument:
                    var order = _fileStorageService.RetrieveTransactionDataAsync<OrderDto>(transaction).Result;
                    return order.NoteData;
                case TransactionTypeEnum.SearchData:
                    var searchOrder = _fileStorageService.RetrieveTransactionDataAsync<OrderDto>(transaction).Result;
                    return searchOrder.SearchData;
                case TransactionTypeEnum.ActionEvent:
                    return _fileStorageService.RetrieveTransactionDataAsync<ReceiveActionEventData>(transaction).Result;
                default:
                    return null;
            }
        }

        public async Task<byte[]> DownloadRequestAsync(string fileNumber)
        {
            var trxn = await _dbContext.Transactions
                .Where(t => t.FileNumber == fileNumber)
                .OrderByDescending(t => t.ReceivedTime)
                .Select(t => new
                {
                    Path = t.DataPath
                })
                .FirstOrDefaultAsync();

            if (trxn != null && !string.IsNullOrEmpty(trxn.Path))
            {
                var requestFileData = _fileStorageService.GetRequestData(trxn.Path);

                byte[] fileBytes = Encoding.UTF8.GetBytes(requestFileData ?? "");

                return fileBytes;
            }
            else
            {
                throw new ArgumentException($"No transaction found for FileNumber: {fileNumber}");
            }
        }

        public async Task<byte[]> DownloadResponseAsync(string fileNumber, TransactionTypeEnum documentType)
        {
            var trxn = await _dbContext.Transactions
                .Where(t => t.FileNumber == fileNumber)
                .OrderByDescending(t => t.ReceivedTime)
                .Select(t => new
                {
                    Path = t.DataPath
                })
                .FirstOrDefaultAsync();

            if (trxn != null && !string.IsNullOrEmpty(trxn.Path))
            {
                var requestFileData = _fileStorageService.GetResponseData(trxn.Path, documentType);

                byte[] fileBytes = Encoding.UTF8.GetBytes(requestFileData ?? "");

                return fileBytes;
            }
            else
            {
                throw new ArgumentException($"No transaction found for FileNumber: {fileNumber}");
            }
        }

        #endregion Transaction Info

        #region Apply Transaction Data Retention

        public async Task CleanupTransactionDataAsync(BackgroundServiceConfig config, CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (config.RetentionPolicy.TransactionDataCacheDurationInDays > 0)
                {
                    var dataRetentionCutOffDate = DateTime.Now.AddDays(config.RetentionPolicy.TransactionDataCacheDurationInDays * -1);

                    //Get all transactions received before the cutoff date and completed successfully
                    var transactions = _dbContext.Transactions
                        .Include(t => t.Items)
                            .ThenInclude(ti => ti.Responses)
                        .AsSplitQuery()
                        .Where(tx => tx.Processed && tx.ReceivedTime < dataRetentionCutOffDate)
                        .ToList();


                    if (transactions.Any())
                    {
                        foreach (var transaction in transactions)
                        {
                            await _fileStorageService.RemoveTransactionDataAsync(transaction);
                            _dbContext.Remove(transaction);
                        }
                        _dbContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while Cleaning up TransactionData. Error: {Message}", ex.Message);
            }

            /*
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                if(config.RetentionPolicy.TransactionFilesCacheDurationInDays > 0)
                {
                    var fileRetentionCutOffDate = DateTime.Now.AddDays(config.RetentionPolicy.TransactionFilesCacheDurationInDays * -1);
                    await _fileStorageService.CleanupTransactionDataAsync(DateOnly.FromDateTime(fileRetentionCutOffDate));
                }
                if (config.RetentionPolicy.TransactionDataCacheDurationInDays > 0)
                {
                    var dataRetentionCutOffDate = DateTime.Now.AddDays(config.RetentionPolicy.TransactionDataCacheDurationInDays * -1);
                    var transactions = _dbContext.Transactions.Where(tx => tx.ResponseSent == true && tx.ReceivedTime < dataRetentionCutOffDate);
                    if (transactions.Any())
                    {
                        _dbContext.RemoveRange(transactions);
                        _dbContext.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while CleanupTransactionDataAsync. Error: {Message}", ex.Message);
            }
            */
        }

        #endregion Apply Transaction Data Retention
    }
}
