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

        public static Task<LabResponse> GetSpecificUserAsync(string upn)
        {
            var query = new UserQuery();
            query.Upn = upn;
            return GetLabUserDataAsync(query);
        }

        public static Task<LabResponse> GetAdfsUserAsync(FederationProvider federationProvider, bool federated = true)
        {
            var query = UserQuery.DefaultUserQuery;
            query.FederationProvider = federationProvider;
            query.IsFederatedUser = true;
            query.IsFederatedUser = federated;
            return GetLabUserDataAsync(query);
        }

        public static string FetchUserPassword(string passwordUri)
        {
            if (string.IsNullOrWhiteSpace(passwordUri))
            {
                throw new InvalidOperationException("Error: CredentialUrl is not set on user. Password retrieval failed.");
            }

            if (s_keyVaultSecretsProvider == null)
            {
                throw new InvalidOperationException("Error: Keyvault secrets provider is not set");
            }

            try
            {
                var secret = s_keyVaultSecretsProvider.GetSecret(passwordUri);
                return secret.Value;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Test setup: cannot get the user password. See inner exception.", e);
            }
        }
    }
}
