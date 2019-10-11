using System.Threading.Tasks;

namespace ClearCareOnline.Api.Services
{
    public interface IAzureKeyVaultService
    {
        Task<string> GetSecret(string name);
    }
}