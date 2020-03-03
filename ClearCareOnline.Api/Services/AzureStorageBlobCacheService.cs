using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ClearCareOnline.Api.Services
{
    public class AzureStorageBlobCacheService : IAzureStorageBlobCacheService
    {
        private const string _containerName = "clearcare-container";
        private const string _blobName = "agencies.json";

        private readonly ILogger<AzureStorageBlobCacheService> _logger;
        private readonly string _connectionString;

        public AzureStorageBlobCacheService(ILogger<AzureStorageBlobCacheService> logger)
        {
            _logger = logger;
            _connectionString = Environment.GetEnvironmentVariable("APPLICATION_STORAGE_CONNECTION", EnvironmentVariableTarget.Process);
        }

        public async Task<string> RetrieveJsonFromCache()
        {
            var container = new BlobContainerClient(_connectionString, _containerName);
            _ = await container.CreateIfNotExistsAsync();

            try
            {
                var blob = container.GetBlobClient(_blobName);
                BlobDownloadInfo downloadInfo = await blob.DownloadAsync();

                string json = null;
                using (var memoryStream = new MemoryStream())
                {
                    await downloadInfo.Content.CopyToAsync(memoryStream);
                    json = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                }

                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ERROR: Exception occurred when trying to retrieve the json from the Azure Storage Blob, container: {_containerName}, blob: {_blobName}");
                throw;
            }
        }

        public async Task SendJsonToCache(string json)
        {
            var container = new BlobContainerClient(_connectionString, _containerName);
            _ = await container.CreateIfNotExistsAsync();

            try
            {
                using var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                var blob = container.GetBlobClient(_blobName);
                _ = await blob.UploadAsync(memoryStream, true); // overwrite if the blob already exists
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ERROR: Exception occurred when trying to send the json to the Azure Storage Blob, container: {_containerName}, blob: {_blobName}, json: {json}");
                throw;
            }
        }
    }
}
