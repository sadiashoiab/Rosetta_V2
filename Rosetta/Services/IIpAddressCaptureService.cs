using System.Collections.Generic;

namespace Rosetta.Services
{
    public interface IIpAddressCaptureService
    {
        void Add(string ipAddress);
        List<string> GetAddresses();
    }
}