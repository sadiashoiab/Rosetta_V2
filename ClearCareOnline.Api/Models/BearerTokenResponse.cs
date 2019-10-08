using System.Diagnostics.CodeAnalysis;

namespace ClearCareOnline.Api.Models
{
    [ExcludeFromCodeCoverage]
    public class BearerTokenResponse
    {
        public string access_token { get; set; }
        public string scope { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
    }
}