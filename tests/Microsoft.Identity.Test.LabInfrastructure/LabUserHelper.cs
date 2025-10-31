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

        public static Task<object> GetKVLabData(string secret)
        {
            // TODO: Implement caching similar to GetLabUserDataAsync to avoid repeated Key Vault calls
            try
            {
                var keyVaultSecret = KeyVaultSecretsProviderMsal.GetSecretByName(secret);
                string labData = keyVaultSecret.Value;
                
                if (string.IsNullOrEmpty(labData))
                {
                    Debug.WriteLine($"KeyVault secret '{secret}' empty");
                    throw new LabUserNotFoundException(new UserQuery(), $"Found no content for secret '{secret}' in Key Vault.");
                }

                // Check if the value is JSON by trying to parse it
                if (IsValidJson(labData))
                {
                    var response = JsonConvert.DeserializeObject<LabResponse>(labData) ?? throw new LabUserNotFoundException(new UserQuery(), $"Failed to deserialize Key Vault secret '{secret}' to LabResponse.");
                    Debug.WriteLine($"KeyVault '{secret}': {response.User?.Upn ?? response.App?.AppId ?? response.Lab?.TenantId ?? "Unknown"}");
                    return Task.FromResult<object>(response);
                }
                else
                {
                    Debug.WriteLine($"KeyVault '{secret}': raw string ({labData.Length} chars)");
                    return Task.FromResult<object>(labData);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"KeyVault '{secret}' failed: {e.Message}");
                throw new InvalidOperationException($"Failed to retrieve or parse Key Vault secret '{secret}'. See inner exception.", e);
            }

            // Helper method to validate if a string is valid JSON
            static bool IsValidJson(string value)
            {
                try
                {
                    JsonConvert.DeserializeObject(value);
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }
            }
        }

        public static LabResponse MergeKVLabData(params string[] secrets)
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
                    var data = GetKVLabData(secret).Result;
                    
                    if (data is LabResponse labResponse)
                    {
                        if (mergedResponse == null)
                        {
                            mergedResponse = labResponse;
                        }
                        else
                        {
                            mergedResponse = MergeLabResponses(mergedResponse, labResponse);
                        }
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

        [Obsolete("Use GetSpecificUserAsync instead", true)]
        public static Task<LabResponse> GetLabUserDataForSpecificUserAsync(string upn)
        {
            throw new NotSupportedException();
        }

        public static async Task<string> GetMSIEnvironmentVariablesAsync(string uri)
        {
            string result = await s_labService.GetLabResponseAsync(uri).ConfigureAwait(false);
            Debug.WriteLine($"MSI env vars: {result?.Length ?? 0} chars from {uri}");
            return result;
        }
        public static Task<LabResponse> GetDefaultUserAsync()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-Default-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }
        
        public static Task<LabResponse> GetDefaultUserWithMultiTenantAppAsync()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-Default-JSON", "ID4SLAB1", "MSAL-APP-AzureADMultipleOrgs-JSON"));
        }
        public static Task<LabResponse> GetDefaultUser2Async()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-Default2-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }
        public static Task<LabResponse> GetDefaultUser3Async()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-XCG-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }

        public static Task<LabResponse> GetDefaultAdfsUserAsync()
        {
            return Task.FromResult(MergeKVLabData("MSAL-USER-FedDefault-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }

        public static Task<LabResponse> GetMsaUserAsync()
        {
            return GetLabUserDataAsync(UserQuery.MsaUserQuery);
        }

        public static Task<LabResponse> GetHybridSpaAccontAsync()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-Default-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }

        public static Task<LabResponse> GetB2CLocalAccountAsync()
        {
            return GetLabUserDataAsync(UserQuery.B2CLocalAccountUserQuery);
        }

        public static Task<LabResponse> GetB2CFacebookAccountAsync()
        {
            return GetLabUserDataAsync(UserQuery.B2CFacebookUserQuery);
        }

        public static Task<LabResponse> GetB2CGoogleAccountAsync()
        {
            return GetLabUserDataAsync(UserQuery.B2CGoogleUserQuery);
        }

        public static async Task<LabResponse> GetB2CMSAAccountAsync()
        {
            var response = await GetLabUserDataAsync(UserQuery.B2CMSAUserQuery).ConfigureAwait(false);
            if (string.IsNullOrEmpty(response.User.HomeUPN) ||
                string.Equals("None", response.User.HomeUPN, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"B2C MSA HomeUPN set to UPN: {response.User.Upn}");
                response.User.HomeUPN = response.User.Upn;
            }
            return response;
        }

        public static Task<LabResponse> GetSpecificUserAsync(string upn)
        {
            return GetLabUserDataAsync(new UserQuery() { Upn = upn });
        }

        public static Task<LabResponse> GetArlingtonUserAsync()
        {
            var response = GetLabUserDataAsync(UserQuery.ArlingtonUserQuery);
            response.Result.User.AzureEnvironment = AzureEnvironment.azureusgovernment;
            return response;
        }

        public static Task<LabResponse> GetArlingtonADFSUserAsync()
        {
            var query = UserQuery.ArlingtonUserQuery;
            query.UserType = UserType.Federated;
            var response = GetLabUserDataAsync(query);
            response.Result.User.AzureEnvironment = AzureEnvironment.azureusgovernment;
            return response;
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
