using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClearCareOnline.Api
{
    public abstract class BaseMapper<T> : IMapper<T>
    {
        protected readonly IResourceLoader _resourceLoader;

        protected BaseMapper(IResourceLoader resourceLoader)
        {
            _resourceLoader = resourceLoader;
        }

        public abstract Task<IList<T>> Map();
    }
}