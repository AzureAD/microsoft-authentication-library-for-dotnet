// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class LabUserHelper
    {
        private static readonly LabServiceApi s_labService;
        private static readonly KeyVaultSecretsProvider s_keyVaultSecretsProvider;
        private static readonly IDictionary<UserQuery, LabResponse> s_userCache =
            new Dictionary<UserQuery, LabResponse>();


        static LabUserHelper()
        {
            s_keyVaultSecretsProvider = new KeyVaultSecretsProvider();
            s_labService = new LabServiceApi();
        }

        public static async Task<LabResponse> GetLabUserDataAsync(UserQuery query)
        {
            if (s_userCache.ContainsKey(query))
            {
                Debug.WriteLine("User cache hit");
                return s_userCache[query];
            }

            var user = await s_labService.GetLabResponseAsync(query).ConfigureAwait(false);
            if (user == null)
            {
                throw new LabUserNotFoundException(query, "Found no users for the given query.");
            }

            Debug.WriteLine("User cache miss");
            s_userCache.Add(query, user);

            return user;
        }

        public static Task<LabResponse> GetDefaultUserAsync()
        {
            return GetLabUserDataAsync(UserQuery.DefaultUserQuery);
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
            var query = new UserQuery();
            //query.Upn = upn;
            return GetLabUserDataAsync(query);
        }

        public static Task<LabResponse> GetAdfsUserAsync(FederationProvider federationProvider, bool federated = true)
        {
            var query = UserQuery.DefaultUserQuery;
            query.FederationProvider = federationProvider;
            query.UserType = federated ? UserType.Federated : UserType.Cloud;

            if (!federated &&
                federationProvider != FederationProvider.ADFSv2019 )
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

            if (s_keyVaultSecretsProvider == null)
            {
                throw new InvalidOperationException("Error: Keyvault secrets provider is not set");
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
