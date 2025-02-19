﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabUserHelper
    {
        private static readonly LabServiceApi s_labService = new LabServiceApi();
        private static readonly ConcurrentDictionary<UserQuery, LabResponse> s_userCache =
            new ConcurrentDictionary<UserQuery, LabResponse>();

        public static KeyVaultSecretsProvider KeyVaultSecretsProviderMsal { get; private set; }
        public static KeyVaultSecretsProvider KeyVaultSecretsProviderMsid { get; private set; }

        private static bool _isInitialized = false;
        private static readonly object _initializationLock = new object();

        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initializationLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                KeyVaultSecretsProviderMsal = new KeyVaultSecretsProvider(KeyVaultInstance.MsalTeam);
                KeyVaultSecretsProviderMsid = new KeyVaultSecretsProvider(KeyVaultInstance.MSIDLab);

                _isInitialized = true;
            }
        }

        internal static async Task<LabResponse> GetLabUserDataAsync(UserQuery query)
        {
            Initialize();

            if (s_userCache.ContainsKey(query))
            {
                Trace.WriteLine("Lab user cache hit. Selected user: " + s_userCache[query].User.Upn);
                return s_userCache[query];
            }

            var response = await s_labService.GetLabResponseFromApiAsync(query).ConfigureAwait(false);
            if (response == null)
            {
                throw new LabUserNotFoundException("Found no users for the given query: " + query);
            }

            bool added = s_userCache.TryAdd(query, response);
            Debug.WriteLine("User cache miss. Returning user from lab: " + response.User.Upn);
            Debug.WriteLine("User cache updated: " + added);

            return response;
        }

        [Obsolete("Use GetSpecificUserAsync instead", true)]
        public static Task<LabResponse> GetLabUserDataForSpecificUserAsync(string upn)
        {
            throw new NotSupportedException();
        }

        public static async Task<string> GetMSIEnvironmentVariablesAsync(string uri)
        {
            Initialize();
            string result = await s_labService.GetLabResponseAsync(uri).ConfigureAwait(false);
            return result;
        }

        public static Task<LabResponse> GetDefaultUserAsync()
        {
            return GetLabUserDataAsync(UserQuery.PublicAadUserQuery);
        }

        public static Task<LabResponse> GetDefaultUser2Async()
        {
            return GetLabUserDataAsync(UserQuery.PublicAadUser2Query);
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

        public static Task<LabResponse> GetCiamUserAync()
        {
            var query = new UserQuery()
            {
                FederationProvider = FederationProvider.CIAMCUD,
                SignInAudience = SignInAudience.AzureAdMyOrg
            };

            return GetLabUserDataAsync(query);
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
                return s_labService.GetUserSecretAsync(userLabName).Result;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Test setup: cannot get the user password. See inner exception.", e);
            }
        }
    }
}
