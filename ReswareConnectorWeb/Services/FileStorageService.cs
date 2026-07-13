using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Config;
using ReswareConnectorWeb.Data.Entities;
using ReswareConnectorWeb.Enums;
using System.Globalization;
using System.Text.Json;

namespace ReswareConnectorWeb.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageConfig _config;

        public FileStorageService(IOptions<FileStorageConfig> config)
        {
            _config = config.Value;
        }
        public async Task<string> StoreTransactionDataAsync<T>(T requestData, TransactionTypeEnum transactionType)
        {
            var relativePath = Path.Combine(transactionType.ToString(), DateTime.Now.ToString("MM-dd-yyyy"), Guid.NewGuid().ToString());
            var targetPath = Path.Combine(_config.LocalStorageRoot, relativePath);
            var targetFile = Path.Combine(targetPath, "Request.json");

            IOHelper.CreateDirectory(targetPath);

            await IOHelper.CreateJsonFileAsync(requestData, targetFile);

            return relativePath;
        }

        public async Task StoreDataAsync<T>(string fileNumber, long trxnItemId, T requestData, TransactionTypeEnum transactionType, bool isRequest)
        {
            var targetPath = Path.Combine(_config.LocalStorageRoot, fileNumber);
            var targetXmlFile = Path.Combine(targetPath, $"{DateTime.Now.ToString("yyyyMMddHHmmss")}_{trxnItemId}_{transactionType.ToString()}_" + (isRequest ? "Request" : "Response"));

            IOHelper.CreateDirectory(targetPath);
            await IOHelper.CreateXmlFileAsync(requestData, targetXmlFile);
        }

        public async Task<string> StoreTransactionResponseDataAsync<T>(T responseData, string relativePath, TransactionTypeEnum transactionType)
        {
            var targetPath = Path.Combine(_config.LocalStorageRoot, relativePath);

            if (responseData != null)
            {
                var targetFile = Path.Combine(targetPath, $"Response_{transactionType.ToString()}.json");

                IOHelper.CreateDirectory(targetPath);

                await IOHelper.CreateJsonFileAsync(responseData, targetFile);
            }
            return relativePath;
        }

        public async Task<T> RetrieveTransactionDataAsync<T>(Transaction request)
        {
            var targetPath = Path.Combine(_config.LocalStorageRoot, request.DataPath);
            var targetFile = Path.Combine(targetPath, "Request.json");

            var requestData = await IOHelper.LoadJsonFileAsync<T>(targetFile);

            return requestData;
        }

        //public async Task<byte[]> GetDocumentFromTitleHub(string sourceFile)
        //{
        //    var sourceFilePath = Path.Combine(_config.TitleHubDocRoot, sourceFile);
        //    if(File.Exists(sourceFilePath)) 
        //    {
        //        return await IOHelper.ReadAllBytesAsync(sourceFilePath);
        //    }
        //    throw new FileNotFoundException($"File '{sourceFilePath}' not found!");
        //}
        public async Task<byte[]> GetDocumentFromTitleHub(string sourceFile)
        {
            // Normalize the source file path
            string normalizedSourceFile = sourceFile;

            // Remove leading slashes if present
            if (normalizedSourceFile.StartsWith("/") || normalizedSourceFile.StartsWith("\\"))
            {
                normalizedSourceFile = normalizedSourceFile.Substring(1);
            }

            // Replace forward slashes with backslashes for Windows
            normalizedSourceFile = normalizedSourceFile.Replace('/', '\\');

            // Combine with the root path
            var sourceFilePath = Path.Combine(_config.TitleHubDocRoot, normalizedSourceFile);

            // Additional normalization to ensure consistent path separators
            sourceFilePath = Path.GetFullPath(sourceFilePath);

            if (File.Exists(sourceFilePath))
            {
                return await IOHelper.ReadAllBytesAsync(sourceFilePath);
            }

            throw new FileNotFoundException($"File '{sourceFilePath}' not found!");
        }

        public async Task RemoveTransactionDataAsync(Transaction request)
        {
            await Task.Run(() =>
            {
                var targetPath = Path.Combine(_config.LocalStorageRoot, request.DataPath);
                Directory.Delete(targetPath, true);
                var parentDir = Directory.GetParent(targetPath)?.FullName;
                if(!string.IsNullOrEmpty(parentDir) && Directory.GetDirectories(parentDir).Length == 0)
                {
                    Directory.Delete(parentDir);
                }
            });
        }

        public string? GetRequestData(string dataPath)
        {
            var targetPath = Path.Combine(_config.LocalStorageRoot, dataPath);
            var targetFile = Path.Combine(targetPath, "Request.xml");

            if(File.Exists(targetFile))
            {
                return File.ReadAllText(targetFile);
            }
            return null;
        }

        public string? GetResponseData(string dataPath, TransactionTypeEnum documentType)
        {
            var targetPath = Path.Combine(_config.LocalStorageRoot, dataPath);
            var targetFile = Path.Combine(targetPath, $"Response_{documentType.ToString()}.xml");

            if (File.Exists(targetFile))
            {
                return File.ReadAllText(targetFile);
            }
            return null;
        }

        /*
        public async Task CleanupTransactionDataAsync(DateOnly cutoffDate)
        {
            _logger.LogInformation("Applying Transaction File Retention Policy");
            await Task.Run(() =>
            {
                try
                {
                    var folderGroups = Directory.GetDirectories(_config.LocalStorageRoot);


                    foreach (var folder in folderGroups)
                    {
                        var datewiseFoldersToDelete = Directory.GetDirectories(folder);
                        foreach (var dateFolder in datewiseFoldersToDelete)
                        {
                            try
                            {
                                var folderName = Path.GetFileName(dateFolder);
                                if (DateTime.TryParseExact(folderName, "MM-dd-yyyy",
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var folderDate))
                                {
                                    if (DateOnly.FromDateTime(folderDate) < cutoffDate)
                                    {
                                        Directory.Delete(dateFolder, recursive: true);
                                        _logger.LogInformation($"Deleted folder: {dateFolder}");
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation($"Skipping folder with invalid date format: {folderName}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error deleting folder {dateFolder}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unknow error: {ex.Message}");
                }
            });
            _logger.LogInformation("Applying Transaction File Retention Policy - Completed");
        }
        */
    }
}
