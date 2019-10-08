using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Rosetta.Models
{
    [ExcludeFromCodeCoverage]
    public class Status
    {
        private readonly IEnumerable<string> _ipAddresses;

        public Status(IEnumerable<string> ipAddresses)
        {
            _ipAddresses = ipAddresses;
        }

        public string status => "active";
        public IEnumerable<string> client_ip_addresses => _ipAddresses;
    }
}