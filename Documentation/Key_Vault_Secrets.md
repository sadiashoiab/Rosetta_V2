# Key Vault Secrets

This project requires that several secrets exist in the Key Vault for the Azure App Service to function. These secrets are created automattically when running the terrafrom script.

| Secret Name                 | Secret Description                                                                               |
|:----------------------------|:-------------------------------------------------------------------------------------------------|
| ClearCareClientId           | The ClearCare Online API Client Id                                                               |
| ClearCareClientSecret       | The ClearCare Online API Client Secret                                                           |
| ClearCareUsername           | The ClearCare Online API Username                                                                |
| ClearCarePassword           | The ClearCare Online API Password                                                                |
| CacheExpirationInSec        | The time in seconds before the internal cache expires the cached api.clearcareonline.com results |
| ManuallyMappedFrancisesJson | If the api.clearcareonline.com results need to have manual overrides for a specific franchise, you can add a json object to the array. This will only augment the specific franchise call; the all agencies and all franchise calls will not be affected. The specific franchise call will check to see if the franchise being requested is in the map, if it is in the map, it will give back the map results. If it is not in the map, it will return the first match it finds. |

## ManuallyMappedFranchiseJson

The ManuallyMappedFranchiseJson Secret is just a string. The Azure App Service is expecting the string to be a JSON array of RosettaFranchise objects. By default the terrafrom will add

```JSON
[{"franchise_number":"244","clear_care_agency":3465}]
```

If you would like to remove the default mapping, you would need to change the JSON to be an empty array like:

```JSON
[]
```

If you would like to add another mapping, you would need to add another RosettaFranchise object to the end of the array like:

```JSON
[{"franchise_number":"244","clear_care_agency":3465},{"franchise_number":"997","clear_care_agency":1234}]
```

