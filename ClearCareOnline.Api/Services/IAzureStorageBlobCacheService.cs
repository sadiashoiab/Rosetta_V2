using System.Threading.Tasks;

namespace ClearCareOnline.Api.Services
{
    public interface IAzureStorageBlobCacheService
    {
        Task<string> RetrieveJsonFromCache();
        Task SendJsonToCache(string json);
    }
}
