using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using ClearCareOnline.Api.Services;
using LazyCache;
using Rosetta.Models;

namespace Rosetta.Services
{
    public class RosettaStoneService : IRosettaStoneService
    {
        private const int _twelveHoursAsSeconds = 12 * 60 * 60;
        private const string _cacheKeyPrefix = "_RosettaStoneService_";

        private readonly IAppCache _cache;
        private readonly IMapper<AgencyFranchiseMap> _agencyMapper;
        private readonly IIpAddressCaptureService _ipAddressCaptureService;
        private readonly IAzureKeyVaultService _azureKeyVaultService;

        public RosettaStoneService(IAppCache cache, IMapper<AgencyFranchiseMap> agencyMapper, IIpAddressCaptureService ipAddressCaptureService, IAzureKeyVaultService azureKeyVaultService)
        {
            _cache = cache;
            _agencyMapper = agencyMapper;
            _ipAddressCaptureService = ipAddressCaptureService;
            _azureKeyVaultService = azureKeyVaultService;
        }

        public async Task<int> GetAbsoluteExpiration()
        {
            var expiration = await _azureKeyVaultService.GetSecret("CacheExpirationInSec");
            if (int.TryParse(expiration, out var expirationAsInt))
            {
                return expirationAsInt;
            }

            return _twelveHoursAsSeconds;
        }

        private async Task<IList<RosettaAgency>> RetrieveAgencies()
        {
            var agencies = await _cache.GetAsync<IList<AgencyFranchiseMap>>($"{_cacheKeyPrefix}agencies");

            return agencies
                .Select(agency => new RosettaAgency
                {
                    clear_care_agency = agency.clear_care_agency,
                    franchise_numbers = agency.franchise_numbers.ToArray()
                })
                .OrderBy(franchise => franchise.clear_care_agency)
                .ToList();
        }

        public async Task RefreshCache()
        {
            var expiration = await _cache.GetOrAddAsync($"{_cacheKeyPrefix}expiration", GetAbsoluteExpiration);
            var absoluteExpirationInSeconds = DateTimeOffset.Now.AddSeconds(expiration);
            var agencies = await _agencyMapper.Map();
            _cache.Remove($"{_cacheKeyPrefix}agencies");
            _cache.Add($"{_cacheKeyPrefix}agencies", agencies, absoluteExpirationInSeconds);
        }

        private async Task<IList<RosettaFranchise>> GetManuallyMappedFranchises()
        {
            var manuallyMappedFranchisesJson = await _azureKeyVaultService.GetSecret("ManuallyMappedFranchisesJson");

            try
            {
                var manuallyMappedFranchises = System.Text.Json.JsonSerializer.Deserialize<List<RosettaFranchise>>(manuallyMappedFranchisesJson);
                if (manuallyMappedFranchises.Any())
                {
                    return manuallyMappedFranchises;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return new List<RosettaFranchise>();
        }

        public async Task<Status> GetStatus()
        {
            return await Task.FromResult(new Status(_ipAddressCaptureService.GetAddresses()));
        }

        public async Task<RosettaFranchise> GetFranchise(string franchiseNumber)
        {
            var manuallyMappedFranchises = await _cache.GetOrAddAsync($"{_cacheKeyPrefix}manually", GetManuallyMappedFranchises);
            var manuallyMappedResult = manuallyMappedFranchises.FirstOrDefault(agency => agency.franchise_number.Equals(franchiseNumber));
            if (manuallyMappedResult != null)
            {
                return manuallyMappedResult;
            }

            var agencies = await RetrieveAgencies();
            return agencies.Where(agency => agency.franchise_numbers.Contains(franchiseNumber))
                .Select(match => new RosettaFranchise
                {
                    clear_care_agency = match.clear_care_agency, 
                    franchise_number = franchiseNumber
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
            _cache.Remove($"{_cacheKeyPrefix}expiration");
            _cache.Remove($"{_cacheKeyPrefix}agencies");
            _cache.Remove($"{_cacheKeyPrefix}manually");
            _cache.Remove("_BearerTokenProvider_bearerToken");
        }
    }
}