using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClearCareOnline.Api
{
    public interface IResourceLoader
    {
        Task<IList<T>> LoadAsync<T>(string url);
    }
}