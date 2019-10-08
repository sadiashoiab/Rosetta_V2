using System.Threading.Tasks;

namespace ClearCareOnline.Api
{
    public interface IBearerTokenProvider
    {
        Task<string> RetrieveToken();
    }
}