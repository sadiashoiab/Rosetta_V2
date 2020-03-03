using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClearCareOnline.Api;
using ClearCareOnline.Api.Models;
using ClearCareOnline.Api.Services;
using LazyCache;
using Microsoft.Extensions.Logging;
using Rosetta.Models;

namespace Rosetta.Services
{
    public class RosettaStoneService : IRosettaStoneService
    {
        private const int _twelveHoursAsSeconds = 12 * 60 * 60;
        private const string _cacheKeyPrefix = "_RosettaStoneService_";

        private readonly ILogger<RosettaStoneService> _logger;
        private readonly IAppCache _cache;
        private readonly IMapper<AgencyFranchiseMap> _agencyMapper;
        private readonly IIpAddressCaptureService _ipAddressCaptureService;
        private readonly IAzureKeyVaultService _azureKeyVaultService;
        private readonly IAzureStorageBlobCacheService _azureStorageBlogCacheService;

        public RosettaStoneService(ILogger<RosettaStoneService> logger, IAppCache cache, IMapper<AgencyFranchiseMap> agencyMapper, IIpAddressCaptureService ipAddressCaptureService, IAzureKeyVaultService azureKeyVaultService, IAzureStorageBlobCacheService azureStorageBlogCacheService)
        {
            _logger = logger;
            _cache = cache;
            _agencyMapper = agencyMapper;
            _ipAddressCaptureService = ipAddressCaptureService;
            _azureKeyVaultService = azureKeyVaultService;
            _azureStorageBlogCacheService = azureStorageBlogCacheService;
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

            // note: if cache is empty, return an empty list
            if (agencies == null || !agencies.Any())
            {
                return new List<RosettaAgency>();
            }

            return agencies
                .Select(agency => new RosettaAgency
                {
                    clear_care_agency = agency.clear_care_agency,
                    franchise_numbers = agency.franchise_numbers.ToArray()
                })
                .OrderBy(franchise => franchise.clear_care_agency)
                .ToList();
        }

        public async Task LoadCacheFromStorage()
        {
            _logger.LogInformation("LoadCacheFromStorage: Starting to LoadCacheFromStorage");

            try 
            {
                var expiration = await _cache.GetOrAddAsync($"{_cacheKeyPrefix}expiration", GetAbsoluteExpiration);
                var absoluteExpirationInSeconds = DateTimeOffset.Now.AddSeconds(expiration);
                var json = await _azureStorageBlogCacheService.RetrieveJsonFromCache();
                var agencies = System.Text.Json.JsonSerializer.Deserialize<IList<AgencyFranchiseMap>>(json);
                if (agencies.Any())
                {
                    _logger.LogInformation($"LoadCacheFromStorage: {agencies.Count} agencies were loaded from storage.");
                    _cache.Add($"{_cacheKeyPrefix}agencies", agencies, absoluteExpirationInSeconds);
                }
                else
                {
                    _logger.LogWarning("LoadCacheFromStorage: WARNING: No agencies were loaded from storage.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadCacheFromStorage: ERROR: Exception thrown when trying to LoadCacheFromStorage.");
            }           
            
            _logger.LogInformation("LoadCacheFromStorage: Finished LoadCacheFromStorage");
        }

        private async Task SaveAgenciesToCache(IList<AgencyFranchiseMap> agencies)
        {
            if (agencies.Any())
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(agencies);
                    await _azureStorageBlogCacheService.SendJsonToCache(json);
                    _logger.LogInformation($"SaveAgenciesToCache: {agencies.Count} agencies were saved to storage.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SaveAgenciesToCache: ERROR: Exception thrown when trying to SaveAgenciesToCache.");
                }
            }
            else
            {
                _logger.LogWarning("SaveAgenciesToCache: WARNING: No agencies were saved to storage.");
            }
        }

        public async Task RefreshCache()
        {
            _logger.LogInformation("RefreshCache: Starting to refresh the cache");
            try
            {
                var expiration = await _cache.GetOrAddAsync($"{_cacheKeyPrefix}expiration", GetAbsoluteExpiration);
                var absoluteExpirationInSeconds = DateTimeOffset.Now.AddSeconds(expiration);
                var agencies = await _agencyMapper.Map();
                if (agencies.Any())
                {
                    _logger.LogInformation($"RefreshCache: {agencies.Count} agencies were loaded during cache refresh.");
                    _cache.Add($"{_cacheKeyPrefix}agencies", agencies, absoluteExpirationInSeconds);
                    await SaveAgenciesToCache(agencies);
                }
                else
                {
                    _logger.LogWarning("RefreshCache: WARNING: No agencies were loaded during cache refresh.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshCache: ERROR: Exception thrown when trying to RefreshCache.");
            }   
            _logger.LogInformation("RefreshCache: Finished refreshing the cache");
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