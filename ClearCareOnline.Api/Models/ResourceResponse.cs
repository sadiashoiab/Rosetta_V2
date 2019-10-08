using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ClearCareOnline.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class ResourceResponse<T>
    {
        public int count { get; set; }
        public string next { get; set; }
        public string previous { get; set; }
        public IList<T> results { get; set; }
    }
}