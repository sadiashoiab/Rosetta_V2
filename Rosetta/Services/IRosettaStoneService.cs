using System.Collections.Generic;
using System.Threading.Tasks;
using Rosetta.Models;

namespace Rosetta.Services
{
    public interface IRosettaStoneService
    {
        Task<Status> GetStatus();
        Task<RosettaFranchise> GetFranchise(string franchiseNumber);
        Task<IList<RosettaFranchise>> GetFranchises();
        Task<IList<RosettaAgency>> GetAgencies();
        void ClearCache();
        Task RefreshCache();
        Task<int> GetAbsoluteExpiration();
    }
}