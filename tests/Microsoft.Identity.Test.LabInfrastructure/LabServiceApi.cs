// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Wrapper for new lab service API
    /// </summary>
    public class LabServiceApi : ILabService
    {
        private string _labAccessAppId;
        private string _labAccessClientSecret;
        private string _labApiAccessToken;

        public LabServiceApi()
        {
            KeyVaultSecretsProvider _keyVaultSecretsProvider = new KeyVaultSecretsProvider();
            _labAccessAppId = _keyVaultSecretsProvider.GetMsidLabSecret("LabVaultAppID").Value;
            _labAccessClientSecret = _keyVaultSecretsProvider.GetMsidLabSecret("LabVaultAppSecret").Value;
        }

        /// <summary>
        /// Returns a test user account for use in testing.
        /// </summary>
        /// <param name="query">Any and all parameters that the returned user should satisfy.</param>
        /// <returns>Users that match the given query parameters.</returns>
        public async Task<LabResponse> GetLabResponseAsync(UserQuery query)
        {
            var response = await GetLabResponseFromApiAsync(query).ConfigureAwait(false);
            var user = response.User;

            return response;
        }

        private async Task<LabResponse> GetLabResponseFromApiAsync(UserQuery query)
        {
            //Fetch user
            string result = await RunQueryAsync(query).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new LabUserNotFoundException(query, "No lab user with specified parameters exists");
            }

            return CreateLabResponseFromResultStringAsync(result).Result;
        }

        private async Task<LabResponse> CreateLabResponseFromResultStringAsync(string result)
        {
            LabUser[] userResponses = JsonConvert.DeserializeObject<LabUser[]>(result);
            if (userResponses.Length > 1)
            {
                throw new InvalidOperationException(
                    "Test Setup Error: Not expecting the lab to return multiple users for a query." +
                    " Please have rewrite the query so that it returns a single user.");
            }

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
                App = labApps[0]
            };
        }

        private Task<string> RunQueryAsync(UserQuery query)
        {
            IDictionary<string, string> queryDict = new Dictionary<string, string>();

            //Building user query
            //Required parameters will be set to default if not supplied by the test code
            queryDict.Add(LabApiConstants.MultiFactorAuthentication, query.MFA != null ? query.MFA.ToString() : MFA.None.ToString());
            queryDict.Add(LabApiConstants.ProtectionPolicy, query.ProtectionPolicy != null ? query.ProtectionPolicy.ToString() : ProtectionPolicy.None.ToString());

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

            return SendLabRequestAsync(LabApiConstants.LabEndPoint, queryDict);
        }

        private async Task<string> SendLabRequestAsync(string requestUrl, IDictionary<string, string> queryDict)
        {
            UriBuilder uriBuilder = new UriBuilder(requestUrl)
            {
                Query = string.Join("&", queryDict.Select(x => x.Key + "=" + x.Value.ToString()))
            };

            return await GetLabResponseAsync(uriBuilder.ToString()).ConfigureAwait(false);
        }

        private async Task<string> GetLabResponseAsync(string address)
        {
            if (String.IsNullOrWhiteSpace(_labApiAccessToken))
                _labApiAccessToken = await LabAuthenticationHelper.GetAccessTokenForLabAPIAsync(_labAccessAppId, _labAccessClientSecret).ConfigureAwait(false);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", string.Format(CultureInfo.InvariantCulture, "bearer {0}", _labApiAccessToken));
                return await httpClient.GetStringAsync(address).ConfigureAwait(false);
            }
        }

        public async Task<LabResponse> CreateTempLabUserAsync()
        {
            IDictionary<string, string> queryDict = new Dictionary<string, string>
            {
                { "code", "HC1Tud9RHGK12VoBPH3sbeyyPHfjmACKbyq8bFlhIiEwpMbWYR4zTQ==" },
                { "userType", "Basic" }
            };

            string result = await SendLabRequestAsync(LabApiConstants.CreateLabUser, queryDict).ConfigureAwait(false);
            return CreateLabResponseFromResultStringAsync(result).Result;
        }

        public async Task<string> GetUserSecretAsync(string lab)
        {
            IDictionary<string, string> queryDict = new Dictionary<string, string>
            {
                { "secret", lab }
            };

            string result = await SendLabRequestAsync(LabApiConstants.LabUserCredentialEndpoint, queryDict).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<LabCredentialResponse>(result).Secret;
        }
    }
}
