using System.Collections.Generic;
using System.Linq;

namespace Rosetta.Services
{
    public class IpAddressCaptureService : IIpAddressCaptureService
    {
        private readonly List<string> _ipAddresses;
        public IpAddressCaptureService()
        {
            _ipAddresses = new List<string>();
        }
        public void Add(string ipAddress)
        {
            _ipAddresses.AddRange(new List<string>{ipAddress}.Except(_ipAddresses));
        }

        public List<string> GetAddresses()
        {
            return _ipAddresses;
        }
    }
}