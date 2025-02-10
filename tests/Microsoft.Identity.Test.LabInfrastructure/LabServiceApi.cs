// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Core;
using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Wrapper for new lab service API
    /// </summary>
    public class LabServiceApi 
    {
        private AccessToken? _labApiAccessToken;
        private AccessToken? _msiHelperApiAccessToken;

        public LabServiceApi()
        {
        }

        public async Task<string> GetMSIHelperServiceTokenAsync()
        {
            if (_msiHelperApiAccessToken == null)
            {
                _msiHelperApiAccessToken = await LabAuthenticationHelper
                    .GetAccessTokenForLabAPIAsync()
                    .ConfigureAwait(false);
            }

            return _msiHelperApiAccessToken.Value.Token;
        }

        /// <summary>
        /// Returns a test user account for use in testing.
        /// </summary>
        /// <param name="query">Any and all parameters that the returned user should satisfy.</param>
        /// <returns>Users that match the given query parameters.</returns>

        internal async Task<LabResponse> GetLabResponseFromApiAsync(UserQuery query)
        {
            //Fetch user
            string result = await RunQueryAsync(query).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new LabUserNotFoundException(
                    "No lab user with specified parameters exists: " + query.ToString());
            }

            return CreateLabResponseFromResultStringAsync(result).Result;
        }

        internal async Task<LabResponse> CreateLabResponseFromResultStringAsync(string result)
        {
            LabUser[] userResponses = JsonConvert.DeserializeObject<LabUser[]>(result);

            var user = userResponses[0];

            var appResponse = await GetLabResponseAsync(InternalConstants.LabAppEndpoint + user.AppId).ConfigureAwait(false);
            LabApp[] labApps = JsonConvert.DeserializeObject<LabApp[]>(appResponse);

            var labInfoResponse = await GetLabResponseAsync(InternalConstants.LabInfoEndpoint + user.LabName).ConfigureAwait(false);
            Lab[] labs = JsonConvert.DeserializeObject<Lab[]>(labInfoResponse);

            user.TenantId = labs[0].TenantId;
            user.FederationProvider = labs[0].FederationProvider;

            return new LabResponse
            {
                User = user,
                App = labApps[0],
                Lab = labs[0]
            };
        }

        private Task<string> RunQueryAsync(UserQuery query)
        {
            Dictionary<string, string> queryDict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query.Upn))
            {
                //Building user query
                //Required parameters will be set to default if not supplied by the test code

                queryDict.Add(
                    InternalConstants.MultiFactorAuthentication, 
                    query.MFA != null ? 
                        query.MFA.ToString() : 
                        MFA.None.ToString());

                queryDict.Add(
                    InternalConstants.ProtectionPolicy, 
                    query.ProtectionPolicy != null ? 
                        query.ProtectionPolicy.ToString() : 
                        ProtectionPolicy.None.ToString());

                if (query.UserType != null)
                {
                    queryDict.Add(InternalConstants.UserType, query.UserType.ToString());
                }

                if (query.HomeDomain != null)
                {
                    queryDict.Add(InternalConstants.HomeDomain, query.HomeDomain.ToString());
                }

                if (query.HomeUPN != null)
                {
                    queryDict.Add(InternalConstants.HomeUPN, query.HomeUPN.ToString());
                }

                if (query.B2CIdentityProvider != null)
                {
                    queryDict.Add(InternalConstants.B2CProvider, query.B2CIdentityProvider.ToString());
                }

                if (query.FederationProvider != null)
                {
                    queryDict.Add(InternalConstants.FederationProvider, query.FederationProvider.ToString());
                }

                if (query.AzureEnvironment != null)
                {
                    queryDict.Add(InternalConstants.AzureEnvironment, query.AzureEnvironment.ToString());
                }

                if (query.SignInAudience != null)
                {
                    queryDict.Add(InternalConstants.SignInAudience, query.SignInAudience.ToString());
                }

                if (query.AppPlatform != null)
                {
                    queryDict.Add(InternalConstants.AppPlatform, query.AppPlatform.ToString());
                }

                if (query.PublicClient != null)
                {
                    queryDict.Add(InternalConstants.PublicClient, query.PublicClient.ToString());
                }

                return SendLabRequestAsync(InternalConstants.LabEndPoint, queryDict);
            }
            else
            {
                return SendLabRequestAsync(InternalConstants.LabEndPoint + "/" + query.Upn, queryDict);
            }
        }

        private async Task<string> SendLabRequestAsync(string requestUrl, Dictionary<string, string> queryDict)
        {
            UriBuilder uriBuilder = new UriBuilder(requestUrl);

            if (queryDict.Count > 0)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                uriBuilder.Query = string.Join("&", queryDict?.Select(x => x.Key + "=" + x.Value.ToString()));
#pragma warning restore CA1305 // Specify IFormatProvider
            };

            return await GetLabResponseAsync(uriBuilder.ToString()).ConfigureAwait(false);
        }

        internal async Task<string> GetLabResponseAsync(string address)
        {
            if (_labApiAccessToken == null)
                _labApiAccessToken = await LabAuthenticationHelper.GetAccessTokenForLabAPIAsync().ConfigureAwait(false);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", string.Format(CultureInfo.InvariantCulture, "bearer {0}", _labApiAccessToken.Value.Token));
                return await httpClient.GetStringAsync(address).ConfigureAwait(false);
            }
        }

        internal async Task<string> GetUserSecretAsync(string lab)
        {
            Dictionary<string, string> queryDict = new Dictionary<string, string>
            {
                { "secret", lab }
            };

            string result = await SendLabRequestAsync(InternalConstants.LabUserCredentialEndpoint, queryDict).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<LabCredentialResponse>(result).Secret;
        }

        
    }
}
