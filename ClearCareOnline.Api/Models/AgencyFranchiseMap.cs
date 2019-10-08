using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ClearCareOnline.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class AgencyFranchiseMap
    {
        public int clear_care_agency { get; set; }
        public IList<string> franchise_numbers { get; set; }
    }
}