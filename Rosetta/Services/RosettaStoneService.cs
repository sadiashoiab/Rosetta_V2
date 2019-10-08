using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using LazyCache;
using Rosetta.Models;

namespace Rosetta.Services
{
    public class RosettaStoneService : IRosettaStoneService
    {
        // todo: pull cache absolute expiration from config/environment/keyvault
        private const int _absoluteExpirationInSeconds = 12 * 60 * 60; // 12 hours in seconds, this needs to be longer than what it takes to call the factory to retrieve the results
        private const string _cacheKeyPrefix = "_RosettaStoneService_";

        private readonly IAppCache _cache;
        private readonly IMapper<AgencyFranchiseMap> _agencyMapper;
        private readonly IIpAddressCaptureService _ipAddressCaptureService;

        public RosettaStoneService(IAppCache cache, IMapper<AgencyFranchiseMap> agencyMapper, IIpAddressCaptureService ipAddressCaptureService)
        {
            _cache = cache;
            _agencyMapper = agencyMapper;
            _ipAddressCaptureService = ipAddressCaptureService;
        }

        private async Task<IList<RosettaAgency>> RetrieveAgencies()
        {
            var agencies = await _cache.GetOrAddAsync($"{_cacheKeyPrefix}agencies", _agencyMapper.Map, DateTimeOffset.Now.AddSeconds(_absoluteExpirationInSeconds));
            return agencies
                .Select(agency => new RosettaAgency
                {
                    clear_care_agency = agency.clear_care_agency,
                    franchise_numbers = agency.franchise_numbers.ToArray()
                })
                .OrderBy(franchise => franchise.clear_care_agency)
                .ToList();
        }

        public async Task<Status> GetStatus()
        {
            return await Task.FromResult(new Status(_ipAddressCaptureService.GetAddresses()));
        }

        public async Task<RosettaFranchise> GetFranchise(int franchiseNumber)
        {
            var agencies = await RetrieveAgencies();
            return agencies.Where(agency => agency.franchise_numbers.Contains(franchiseNumber.ToString()))
                .Select(match => new RosettaFranchise
                {
                    clear_care_agency = match.clear_care_agency, 
                    franchise_number = franchiseNumber.ToString()
                })
                .FirstOrDefault();
        }

        public async Task<IList<RosettaFranchise>> GetFranchises()
        {
            var agencies = await RetrieveAgencies();
            var franchiseMappings = new List<RosettaFranchise>();
            foreach (var agency in agencies)
            {
                franchiseMappings.AddRange(agency.franchise_numbers.Select(franchise_number => new RosettaFranchise
                {
                    clear_care_agency = agency.clear_care_agency,
                    franchise_number = franchise_number
                }));
            }

            return franchiseMappings;
        }

        public async Task<IList<RosettaAgency>> GetAgencies()
        {
            var agencies = await RetrieveAgencies();
            return agencies;
        }

        public void ClearCache()
        {
            _cache.Remove($"{_cacheKeyPrefix}agencies");
            _cache.Remove("_BearerTokenProvider_bearerToken");
        }
    }
}