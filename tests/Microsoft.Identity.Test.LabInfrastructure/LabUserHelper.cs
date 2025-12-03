// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabUserHelper
    {
        private static readonly LabServiceApi s_labService;
        private static readonly ConcurrentDictionary<UserQuery, LabResponse> s_userCache =
            new ConcurrentDictionary<UserQuery, LabResponse>();

        public static KeyVaultSecretsProvider KeyVaultSecretsProviderMsal { get; }
        public static KeyVaultSecretsProvider KeyVaultSecretsProviderMsid { get; }

        static LabUserHelper()
        {
            KeyVaultSecretsProviderMsal = new KeyVaultSecretsProvider(KeyVaultInstance.MsalTeam);
            KeyVaultSecretsProviderMsid = new KeyVaultSecretsProvider(KeyVaultInstance.MSIDLab);
            s_labService = new LabServiceApi();
        }

        public static async Task<LabResponse> GetLabUserDataAsync(UserQuery query)
        {
            if (s_userCache.ContainsKey(query))
            {
                var cachedResponse = s_userCache[query];
                Debug.WriteLine($"Lab cache hit: {cachedResponse.User?.Upn ?? "N/A"} | {cachedResponse.App?.AppId ?? "N/A"} | {cachedResponse.Lab?.TenantId ?? "N/A"}");
                return cachedResponse;
            }

            var response = await s_labService.GetLabResponseFromApiAsync(query).ConfigureAwait(false);
            if (response == null)
            {
                Debug.WriteLine($"Lab API returned null for query: {query}");
                throw new LabUserNotFoundException(query, "Found no users for the given query.");
            }

            Debug.WriteLine($"Lab API: {response.User?.Upn ?? "N/A"} | {response.App?.AppId ?? "N/A"} | {response.Lab?.TenantId ?? "N/A"} | {response.User?.AzureEnvironment.ToString() ?? "N/A"}");

            s_userCache.TryAdd(query, response);
            return response;
        }

        private static async Task<LabResponse> GetKVLabDataAsync(string secret)
        {
            // TODO: Implement caching similar to GetLabUserDataAsync to avoid repeated Key Vault calls
            try
            {
                var keyVaultSecret = await KeyVaultSecretsProviderMsal.GetSecretByNameAsync(secret).ConfigureAwait(false);
                string labData = keyVaultSecret.Value;
                
                if (string.IsNullOrEmpty(labData))
                {
                    Debug.WriteLine($"KeyVault secret '{secret}' empty");
                    throw new LabUserNotFoundException(new UserQuery(), $"Found no content for secret '{secret}' in Key Vault.");
                }

                try
                {
                    // Parse JSON directly - let JsonException bubble up if invalid
                    var response = JsonConvert.DeserializeObject<LabResponse>(labData) ?? throw new LabUserNotFoundException(new UserQuery(), $"Failed to deserialize Key Vault secret '{secret}' to LabResponse.");
                    Debug.WriteLine($"KeyVault '{secret}': {response.User?.Upn ?? response.App?.AppId ?? response.Lab?.TenantId ?? "Unknown"}");
                    return response;
                }
                catch (JsonException jsonEx)
                {
                    Debug.WriteLine($"KeyVault '{secret}': invalid JSON ({labData.Length} chars) - {jsonEx.Message}");
                    throw new LabUserNotFoundException(new UserQuery(), $"Key Vault secret '{secret}' contains invalid JSON for LabResponse. {jsonEx.Message}");
                }
            }
            catch (Exception e) when (!(e is LabUserNotFoundException))
            {
                Debug.WriteLine($"KeyVault '{secret}' failed: {e.Message}");
                throw new InvalidOperationException($"Failed to retrieve or parse Key Vault secret '{secret}'. See inner exception.", e);
            }
        }

        public static async Task<LabResponse> MergeKVLabDataAsync(params string[] secrets)
        {
            if (secrets == null || secrets.Length == 0)
            {
                throw new ArgumentException("At least one secret name must be provided.", nameof(secrets));
            }

            try
            {
                LabResponse mergedResponse = null;
                
                foreach (string secret in secrets)
                {
                    var labResponse = await GetKVLabDataAsync(secret).ConfigureAwait(false);
                    
                    if (mergedResponse == null)
                    {
                        mergedResponse = labResponse;
                    }
                    else
                    {
                        mergedResponse = MergeLabResponses(mergedResponse, labResponse);
                    }
                }

                if (mergedResponse == null)
                {
                    Debug.WriteLine($"Merge failed - no valid LabResponse in: {string.Join(", ", secrets)}");
                    throw new LabUserNotFoundException(new UserQuery(), $"Failed to create merged LabResponse from secrets: {string.Join(", ", secrets)}");
                }

                Debug.WriteLine($"Merged [{string.Join(", ", secrets)}]: {mergedResponse.User?.Upn ?? "N/A"} | {mergedResponse.App?.AppId ?? "N/A"} | {mergedResponse.Lab?.TenantId ?? "N/A"}");
                return mergedResponse;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Merge failed [{string.Join(", ", secrets)}]: {e.Message}");
                throw new InvalidOperationException($"Failed to merge Key Vault secrets: {string.Join(", ", secrets)}. See inner exception.", e);
            }
        }

        private static LabResponse MergeLabResponses(LabResponse primary, LabResponse secondary)
        {
            var primaryJson = JObject.FromObject(primary);
            var secondaryJson = JObject.FromObject(secondary);
            
            primaryJson.Merge(secondaryJson, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Ignore
            });
            
            return primaryJson.ToObject<LabResponse>();
        }
     
        public static async Task<string> GetMSIEnvironmentVariablesAsync(string uri)
        {
            string result = await s_labService.GetLabResponseAsync(uri).ConfigureAwait(false);
            Debug.WriteLine($"MSI env vars: {result?.Length ?? 0} chars from {uri}");
            return result;
        }
        public static Task<LabResponse> GetDefaultUserAsync()
        {
            return MergeKVLabDataAsync("MSAL-User-Default-JSON", "ID4SLAB1", "MSAL-App-Default-JSON");
        }
        
        public static Task<LabResponse> GetDefaultUserWithMultiTenantAppAsync()
        {
            return MergeKVLabDataAsync("MSAL-User-Default-JSON", "ID4SLAB1", "MSAL-APP-AzureADMultipleOrgs-JSON");
        }
        public static Task<LabResponse> GetDefaultUser2Async()
        {
            return MergeKVLabDataAsync("MSAL-User-Default2-JSON", "ID4SLAB1", "MSAL-App-Default-JSON");
        }
        public static Task<LabResponse> GetDefaultUser3Async()
        {
            return MergeKVLabDataAsync("MSAL-User-XCG-JSON", "ID4SLAB1", "MSAL-App-Default-JSON");
        }

        public static Task<LabResponse> GetDefaultAdfsUserAsync()
        {
            return MergeKVLabDataAsync("MSAL-USER-FedDefault-JSON", "ID4SLAB1", "MSAL-App-Default-JSON");
        }

        public static Task<LabResponse> GetHybridSpaAccontAsync()
        {
            return MergeKVLabDataAsync("MSAL-User-Default-JSON", "ID4SLAB1", "MSAL-App-Default-JSON");
        }

        public static Task<LabResponse> GetB2CLocalAccountAsync()
        {
            return MergeKVLabDataAsync("B2C-User-IDLab-JSON", "MSIDLABB2C", "B2C-App-IDLABSAPPB2C-JSON");
        }

        public static Task<LabResponse> GetArlingtonUserAsync()
        {
            var response = MergeKVLabDataAsync("ARL-User-IDLab-JSON", "ARLMSIDLAB1", "ARL-App-IDLABSAPP-JSON");
            response.Result.User.AzureEnvironment = AzureEnvironment.azureusgovernment;
            return response;
        }

        public static Task<LabResponse> GetArlingtonADFSUserAsync()
        {
            var response = MergeKVLabDataAsync("ARL-User-fIDLAB-JSON", "ARLMSIDLAB1", "ARL-App-IDLABSAPP-JSON");
            response.Result.User.AzureEnvironment = AzureEnvironment.azureusgovernment;
            return response;
        }
        public static Task<LabResponse> GetCIAMUserAsync()
        {
            return MergeKVLabDataAsync("MSAL-User-CIAM-JSON", "MSIDLABCIAM6", "MSAL-App-CIAM-JSON");
        }
 
        public static Task<LabResponse> GetAdfsUserAsync(FederationProvider federationProvider, bool federated = true)
        {
            var query = new UserQuery()
            {
                AzureEnvironment = LabInfrastructure.AzureEnvironment.azurecloud,
                FederationProvider = federationProvider,
                UserType = federated ? UserType.Federated : UserType.Cloud
            };

            if (!federated && federationProvider != FederationProvider.ADFSv2019)
            {
                Debug.WriteLine($"Invalid ADFS config: {federationProvider} non-federated not supported");
                throw new InvalidOperationException("Test Setup Error: MSAL only supports ADFS2019 direct (non-federated) access. " +
                    "Support for older versions of ADFS is exclusively via federation");
            }

            return GetLabUserDataAsync(query);
        }

        public static string FetchUserPassword(string userLabName)
        {
            // TODO: Implement caching similar to GetLabUserDataAsync to avoid repeated Key Vault calls
            if (string.IsNullOrWhiteSpace(userLabName))
            {
                Debug.WriteLine("Password fetch failed: empty lab name");
                throw new InvalidOperationException("Error: lab name is not set on user. Password retrieval failed.");
            }

            if (KeyVaultSecretsProviderMsid == null || KeyVaultSecretsProviderMsal == null)
            {
                Debug.WriteLine("Password fetch failed: KeyVault provider not initialized");
                throw new InvalidOperationException("Error: KeyVault secrets provider is not set");
            }

            try
            {
                var keyVaultSecret = KeyVaultSecretsProviderMsid.GetSecretByName(userLabName);
                string password = keyVaultSecret.Value;
                
                if (!string.IsNullOrEmpty(password))
                {
                    Debug.WriteLine($"Password retrieved for {userLabName} ({password.Length} chars)");
                    return password;
                }
                
                Debug.WriteLine($"Password empty for {userLabName}");
                throw new InvalidOperationException($"Password secret '{userLabName}' found but was empty in Key Vault.");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Password fetch failed for {userLabName}: {e.Message}");
                throw new InvalidOperationException($"Test setup: cannot get the user password from Key Vault secret '{userLabName}'. See inner exception.", e);
            }
        }
    }
}
