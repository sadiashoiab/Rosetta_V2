using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClearCareOnline.Api
{
    public interface IMapper<T>
    {
        Task<IList<T>> Map();
    }
}