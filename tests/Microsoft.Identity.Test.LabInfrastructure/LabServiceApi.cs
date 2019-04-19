// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Wrapper for new lab service API
    /// </summary>
    public class LabServiceApi : ILabService, IDisposable
    {
        private readonly HttpClient _httpClient;

        public LabServiceApi()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Returns a test user account for use in testing.
        /// </summary>
        /// <param name="query">Any and all parameters that the returned user should satisfy.</param>
        /// <returns>Users that match the given query parameters.</returns>
        public LabResponse GetLabResponse(UserQuery query)
        {
            var response = GetLabResponseFromApi(query);
            var user = response.User;

            if (!Uri.IsWellFormedUriString(user.CredentialUrl, UriKind.Absolute))
            {
                Console.WriteLine($"User '{user.Upn}' has invalid Credential URL: '{user.CredentialUrl}'");
            }

            if (user.IsExternal && user.HomeUser == null)
            {
                Console.WriteLine($"User '{user.Upn}' has no matching home user.");
            }

            return response;
        }

        private LabResponse GetLabResponseFromApi(UserQuery query)
        {
            //Fetch user
            string result = RunQuery(query);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new LabUserNotFoundException(query, "No lab user with specified parameters exists");
            }

            LabResponse response = JsonConvert.DeserializeObject<LabResponse>(result);
            LabUser user = JsonConvert.DeserializeObject<LabUser>(result);

            if (!string.IsNullOrEmpty(user.HomeTenantId) && !string.IsNullOrEmpty(user.HomeUPN))
            {
                user.InitializeHomeUser();
            }

            return response;
        }

        private string RunQuery(UserQuery query)
        {
            IDictionary<string, string> queryDict = new Dictionary<string, string>();
            UriBuilder uriBuilder = new UriBuilder(LabApiConstants.LabEndpoint);

            //Disabled for now until there are tests that use it.
            queryDict.Add(LabApiConstants.MobileAppManagementWithConditionalAccess, LabApiConstants.False);
            queryDict.Add(LabApiConstants.MobileDeviceManagementWithConditionalAccess, LabApiConstants.False);

            //Building user query
            if (query.FederationProvider != null)
            {
                queryDict.Add(LabApiConstants.FederationProvider, query.FederationProvider.ToString());
            }

            if (!string.IsNullOrWhiteSpace(query.Upn))
            {
                queryDict.Add(LabApiConstants.Upn, query.Upn);
            }

            queryDict.Add(LabApiConstants.MobileAppManagement, query.IsMamUser != null && (bool)(query.IsMamUser) ? LabApiConstants.True : LabApiConstants.False);
            queryDict.Add(LabApiConstants.MultiFactorAuthentication, query.IsMfaUser != null && (bool)(query.IsMfaUser) ? LabApiConstants.True : LabApiConstants.False);

            if (query.Licenses != null && query.Licenses.Count > 0)
            {
                queryDict.Add(LabApiConstants.License, query.Licenses.ToArray().ToString());
            }

            queryDict.Add(LabApiConstants.FederatedUser, query.IsFederatedUser != null && (bool)(query.IsFederatedUser) ? LabApiConstants.True : LabApiConstants.False);

            if (query.UserType != null)
            {
                queryDict.Add(LabApiConstants.UserType, query.UserType.ToString());
            }

            queryDict.Add(LabApiConstants.External, query.IsExternalUser != null && (bool)(query.IsExternalUser) ? LabApiConstants.True : LabApiConstants.False);

            if (query.B2CIdentityProvider == B2CIdentityProvider.Local)
            {
                queryDict.Add(LabApiConstants.B2CProvider, LabApiConstants.B2CLocal);
            }

            if (query.B2CIdentityProvider == B2CIdentityProvider.Facebook)
            {
                queryDict.Add(LabApiConstants.B2CProvider, LabApiConstants.B2CFacebook);
            }

            if (query.B2CIdentityProvider == B2CIdentityProvider.Google)
            {
                queryDict.Add(LabApiConstants.B2CProvider, LabApiConstants.B2CGoogle);
            }

            uriBuilder.Query = string.Join("&", queryDict.Select(x => x.Key + "=" + x.Value.ToString()));
            string result = _httpClient.GetStringAsync(uriBuilder.ToString()).GetAwaiter().GetResult();

            return result;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
