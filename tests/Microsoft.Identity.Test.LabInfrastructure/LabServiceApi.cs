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
        private string _labAccessAppId;
        private string _labAccessClientSecret;
        private string _msiHelperServiceSecret;
        private AccessToken? _labApiAccessToken;
        private AccessToken? _msiHelperApiAccessToken;

        public LabServiceApi()
        {
            KeyVaultSecretsProvider _keyVaultSecretsProvider = new KeyVaultSecretsProvider();
            _labAccessAppId = _keyVaultSecretsProvider.GetSecretByName("LabVaultAppID").Value;
            _labAccessClientSecret = _keyVaultSecretsProvider.GetSecretByName("LabVaultAppSecret").Value;
            _msiHelperServiceSecret = _keyVaultSecretsProvider.GetSecretByName("MSIHelperServiceSecret").Value;
        }

        /// <summary>
        /// Returns a test user account for use in testing.
        /// </summary>
        /// <param name="query">Any and all parameters that the returned user should satisfy.</param>
        /// <returns>Users that match the given query parameters.</returns>

        public async Task<LabResponse> GetLabResponseFromApiAsync(UserQuery query)
        {
            //Fetch user
            string result = await RunQueryAsync(query).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new LabUserNotFoundException(query, "No lab user with specified parameters exists");
            }

            return CreateLabResponseFromResultStringAsync(result).Result;
        }

        internal async Task<LabResponse> CreateLabResponseFromResultStringAsync(string result)
        {
            LabUser[] userResponses = JsonConvert.DeserializeObject<LabUser[]>(result);

            var user = userResponses[0];

            var appResponse = await GetLabResponseAsync(LabApiConstants.LabAppEndpoint + user.AppId).ConfigureAwait(false);
            LabApp[] labApps = JsonConvert.DeserializeObject<LabApp[]>(appResponse);

            var labInfoResponse = await GetLabResponseAsync(LabApiConstants.LabInfoEndpoint + user.LabName).ConfigureAwait(false);
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
                    LabApiConstants.MultiFactorAuthentication, 
                    query.MFA != null ? 
                        query.MFA.ToString() : 
                        MFA.None.ToString());

                queryDict.Add(
                    LabApiConstants.ProtectionPolicy, 
                    query.ProtectionPolicy != null ? 
                        query.ProtectionPolicy.ToString() : 
                        ProtectionPolicy.None.ToString());

                if (query.UserType != null)
                {
                    queryDict.Add(LabApiConstants.UserType, query.UserType.ToString());
                }

                if (query.HomeDomain != null)
                {
                    queryDict.Add(LabApiConstants.HomeDomain, query.HomeDomain.ToString());
                }

                if (query.HomeUPN != null)
                {
                    queryDict.Add(LabApiConstants.HomeUPN, query.HomeUPN.ToString());
                }

                if (query.B2CIdentityProvider != null)
                {
                    queryDict.Add(LabApiConstants.B2CProvider, query.B2CIdentityProvider.ToString());
                }

                if (query.FederationProvider != null)
                {
                    queryDict.Add(LabApiConstants.FederationProvider, query.FederationProvider.ToString());
                }

                if (query.AzureEnvironment != null)
                {
                    queryDict.Add(LabApiConstants.AzureEnvironment, query.AzureEnvironment.ToString());
                }

                if (query.SignInAudience != null)
                {
                    queryDict.Add(LabApiConstants.SignInAudience, query.SignInAudience.ToString());
                }

                if (query.AppPlatform != null)
                {
                    queryDict.Add(LabApiConstants.AppPlatform, query.AppPlatform.ToString());
                }

                if (query.PublicClient != null)
                {
                    queryDict.Add(LabApiConstants.PublicClient, query.PublicClient.ToString());
                }

                return SendLabRequestAsync(LabApiConstants.LabEndPoint, queryDict);
            }
            else
            {
                return SendLabRequestAsync(LabApiConstants.LabEndPoint + "/" + query.Upn, queryDict);
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
                _labApiAccessToken = await LabAuthenticationHelper.GetAccessTokenForLabAPIAsync(_labAccessAppId, _labAccessClientSecret).ConfigureAwait(false);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", string.Format(CultureInfo.InvariantCulture, "bearer {0}", _labApiAccessToken.Value.Token));
                return await httpClient.GetStringAsync(address).ConfigureAwait(false);
            }
        }

        public async Task<string> GetUserSecretAsync(string lab)
        {
            Dictionary<string, string> queryDict = new Dictionary<string, string>
            {
                { "secret", lab }
            };

            string result = await SendLabRequestAsync(LabApiConstants.LabUserCredentialEndpoint, queryDict).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<LabCredentialResponse>(result).Secret;
        }

        public async Task<string> GetMSIHelperServiceTokenAsync()
        {
            if (_msiHelperApiAccessToken == null)
            {
                _msiHelperApiAccessToken = await LabAuthenticationHelper
                    .GetAccessTokenForLabAPIAsync(_labAccessAppId, _msiHelperServiceSecret)
                    .ConfigureAwait(false);
            }

            return _msiHelperApiAccessToken.Value.Token;
        }
    }
}
