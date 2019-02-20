//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

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
    public class LabServiceApi : ILabService
    {
        private readonly KeyVaultSecretsProvider _keyVault;

        public LabServiceApi(KeyVaultSecretsProvider keyVault)
        {
            _keyVault = keyVault;
        }

        private LabResponse GetLabResponseFromApi(UserQuery query)
        {
            //Fetch user
            string result = CreateLabQuery(query);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new LabUserNotFoundException(query, "No lab user with specified parameters exists");
            }

            LabResponse response = JsonConvert.DeserializeObject<LabResponse>(result);

            LabUser user = response.User;

            user = JsonConvert.DeserializeObject<LabUser>(result);

            if (!string.IsNullOrEmpty(user.HomeTenantId) && !string.IsNullOrEmpty(user.HomeUPN))
            {
                user.InitializeHomeUser();
            }

            return response;
        }

        private string CreateLabQuery(UserQuery query)
        {
            bool queryRequiresBetaEndpoint = false;
            HttpClient webClient = new HttpClient();
            IDictionary<string, string> queryDict = new Dictionary<string, string>();

            //Disabled for now until there are tests that use it.
            queryDict.Add(LabApiConstants.MobileAppManagementWithConditionalAccess, LabApiConstants.False);
            queryDict.Add(LabApiConstants.MobileDeviceManagementWithConditionalAccess, LabApiConstants.False);

            //Building user query
            if (query.FederationProvider != null)
            {
                if (query.FederationProvider == FederationProvider.ADFSv2019)
                {
                    queryRequiresBetaEndpoint = true;
                }
                queryDict.Add(LabApiConstants.FederationProvider, query.FederationProvider.ToString());
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

            if (!string.IsNullOrEmpty(query.UserSearch))
            {
                queryDict.Add(LabApiConstants.UserSearchQuery, query.UserSearch);
                queryRequiresBetaEndpoint = true;
            }

            if (!string.IsNullOrEmpty(query.AppName))
            {
                queryDict.Add(LabApiConstants.AppName, query.AppName);
                queryRequiresBetaEndpoint = true;
            }

            UriBuilder uriBuilder = queryRequiresBetaEndpoint? new UriBuilder(LabApiConstants.BetaEndpoint) : new UriBuilder(LabApiConstants.LabEndpoint);

            uriBuilder.Query = string.Join("&", queryDict.Select(x => x.Key + "=" + x.Value.ToString()));
            return webClient.GetStringAsync(uriBuilder.ToString()).GetAwaiter().GetResult();
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
    }
}