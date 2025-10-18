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
                Trace.WriteLine("Lab user cache hit. Selected user: " + s_userCache[query].User.Upn);
                return s_userCache[query];
            }

            var response = await s_labService.GetLabResponseFromApiAsync(query).ConfigureAwait(false);
            if (response == null)
            {
                throw new LabUserNotFoundException(query, "Found no users for the given query.");
            }

            bool added = s_userCache.TryAdd(query, response);
            Debug.WriteLine("User cache miss. Returning user from lab: " + response.User.Upn);
            Debug.WriteLine("User cache updated: " + added);

            return response;
        }

        public static Task<object> GetKVLabData(string secret)
        {
            try
            {
                var keyVaultSecret = KeyVaultSecretsProviderMsal.GetSecretByName(secret);
                string labData = keyVaultSecret.Value;
                
                if (string.IsNullOrEmpty(labData))
                {
                    throw new LabUserNotFoundException(new UserQuery(), $"Found no content for secret '{secret}' in Key Vault.");
                }

                // Check if the value is JSON by trying to parse it
                if (IsValidJson(labData))
                {
                    var response = JsonConvert.DeserializeObject<LabResponse>(labData) ?? throw new LabUserNotFoundException(new UserQuery(), $"Failed to deserialize Key Vault secret '{secret}' to LabResponse.");

                    Debug.WriteLine($"Key Vault lab data retrieved from secret '{secret}' (JSON): " + 
                        (response.User?.Upn ?? response.App?.AppId ?? response.Lab?.TenantId ?? "Unknown"));
                    return Task.FromResult<object>(response);
                }
                else
                {
                    // Return raw string value if not JSON
                    Debug.WriteLine($"Key Vault secret '{secret}' retrieved (raw): {labData}");
                    return Task.FromResult<object>(labData);
                }
            }
            catch (Exception e)
            {
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
                            // First LabResponse becomes the base
                            mergedResponse = labResponse;
                        }
                        else
                        {
                            // Merge subsequent LabResponses
                            mergedResponse = MergeLabResponses(mergedResponse, labResponse);
                        }
                    }
                    else if (data is string rawValue)
                    {
                        // Handle raw string values if needed
                        Debug.WriteLine($"Skipping raw string value from secret '{secret}': {rawValue}");
                    }
                }

                if (mergedResponse == null)
                {
                    throw new LabUserNotFoundException(new UserQuery(), $"Failed to create merged LabResponse from secrets: {string.Join(", ", secrets)}");
                }

                Debug.WriteLine($"Merged lab data from secrets [{string.Join(", ", secrets)}]: " + 
                    (mergedResponse.User?.Upn ?? mergedResponse.App?.AppId ?? mergedResponse.Lab?.TenantId ?? "Unknown"));
                
                return mergedResponse;
            }
            catch (Exception e)
            {
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
            return result;
        }
        public static Task<LabResponse> GetDefaultUserAsync()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-Default-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }
        public static Task<LabResponse> GetDefaultUser2Async()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-Default2-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }
        public static Task<LabResponse> GetDefaultUser3Async()
        {
            return Task.FromResult(MergeKVLabData("MSAL-User-XCG-JSON", "ID4SLAB1", "MSAL-App-Default-JSON"));
        }

        public static Task<LabResponse> GetMsaUserAsync()
        {
            return GetLabUserDataAsync(UserQuery.MsaUserQuery);
        }

        public static Task<LabResponse> GetHybridSpaAccontAsync()
        {
            return GetLabUserDataAsync(UserQuery.HybridSpaUserQuery);
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

            if (!federated &&
                federationProvider != FederationProvider.ADFSv2019)
            {
                throw new InvalidOperationException("Test Setup Error: MSAL only supports ADFS2019 direct (non-federated) access. " +
                    "Support for older versions of ADFS is exclusively via federation");
            }

            return GetLabUserDataAsync(query);
        }

        public static string FetchUserPassword(string userLabName)
        {
            if (string.IsNullOrWhiteSpace(userLabName))
            {
                throw new InvalidOperationException("Error: lab name is not set on user. Password retrieval failed.");
            }

            if (KeyVaultSecretsProviderMsid == null || KeyVaultSecretsProviderMsal == null)
            {
                throw new InvalidOperationException("Error: KeyVault secrets provider is not set");
            }

            try
            {
                // Try to fetch password from MSIDLab Key Vault first
                var keyVaultSecret = KeyVaultSecretsProviderMsid.GetSecretByName(userLabName);
                string password = keyVaultSecret.Value;
                
                if (!string.IsNullOrEmpty(password))
                {
                    Debug.WriteLine($"Password retrieved from Key Vault for: {userLabName}");
                    return password;
                }
                
                throw new InvalidOperationException($"Password secret '{userLabName}' found but was empty in Key Vault.");
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Test setup: cannot get the user password from Key Vault secret '{userLabName}'. See inner exception.", e);
            }
        }
    }
}
