using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClearCareOnline.Api.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ClearCareOnline.Api
{
    public class AgencyMapper : BaseMapper<AgencyFranchiseMap>
    {
        private readonly string _agenciesUrl;
        private readonly Regex _oneOrMoreDigitsRegex = new Regex(@"\d+", RegexOptions.Compiled);

        public AgencyMapper(IConfiguration configuration, IResourceLoader resourceLoader) : base(resourceLoader)
        {
            _agenciesUrl = configuration["ClearCareAgenciesUrl"];
        }

        public override async Task<IList<AgencyFranchiseMap>> Map()
        {
            var agencies = await _resourceLoader.LoadAsync<AgencyResponse>(_agenciesUrl);
            var franchiseMapping = await Transform(agencies);
            return franchiseMapping.OrderBy(i => i.clear_care_agency).ToList();
        }

        private async Task<IList<AgencyFranchiseMap>> Transform(IList<AgencyResponse> agencies)
        {
            var transformResults = new List<AgencyFranchiseMap>();
            foreach (var agency in agencies)
            {
                var transformResult = await TransformAndMapAgency(agency);
                transformResults.Add(transformResult);
            }

            var results = transformResults.Where(result => result.franchise_numbers.Any()).ToList();
            return results;
        }
 
        private async Task<AgencyFranchiseMap> TransformAndMapAgency(AgencyResponse clearCareAgencyResult)
        {
            var franchiseMap = new AgencyFranchiseMap
            {
                clear_care_agency = clearCareAgencyResult.id
            };

            string subdomainFranchise = null;
            var containsOnOrMoreDigits = _oneOrMoreDigitsRegex.Match(clearCareAgencyResult.subdomain);
            if (containsOnOrMoreDigits.Success)
            {
                subdomainFranchise = containsOnOrMoreDigits.Value;
            }

            IList<LocationResponse> locations = new List<LocationResponse>();
            if (!string.IsNullOrWhiteSpace(clearCareAgencyResult.locations))
            {
                locations = await _resourceLoader.LoadAsync<LocationResponse>(clearCareAgencyResult.locations);
            }

            franchiseMap.franchise_numbers = GetFranchiseNumbers(subdomainFranchise, locations);
            return franchiseMap;
        }

        private IList<string> GetFranchiseNumbers(string subdomainFranchise, IList<LocationResponse> locations)
        {
            var franchises = new List<string>();

            if (!string.IsNullOrWhiteSpace(subdomainFranchise))
            {
                franchises.Add(subdomainFranchise);
            }
            
            if (locations.Any())
            {
                franchises.AddRange(locations.Where(location => !location.name.Equals(subdomainFranchise) && location.name.All(char.IsDigit)).Select(location => location.name));
            }

            return franchises;
        }
    }
}