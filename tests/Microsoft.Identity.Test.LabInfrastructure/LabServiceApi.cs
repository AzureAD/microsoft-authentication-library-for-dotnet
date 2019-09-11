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

            if (!Uri.IsWellFormedUriString(user.CredentialUrl, UriKind.Absolute))
            {
                Console.WriteLine($"User '{user.Upn}' has invalid Credential URL: '{user.CredentialUrl}'");
            }

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

            return CreateLabResponseFromResultString(result);
        }

        private static LabResponse CreateLabResponseFromResultString(string result)
        {
            LabResponse[] responses = JsonConvert.DeserializeObject<LabResponse[]>(result);
            if (responses.Length > 1)
            {
                throw new InvalidOperationException(
                    "Test Setup Error: Not expecting the lab to return multiple users for a query." +
                    " Please have rewrite the query so that it returns a single user.");
            }

            var response = responses[0];
            response.User.CredentialUrl = response.Lab.CredentialVaultkeyName;
            response.User.TenantId = response.Lab.TenantId;
            response.User.FederationProvider = response.Lab.FederationProvider;

            return response;
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

            var res = await LabAuthenticationHelper.GetAccessTokenForLabAPIAsync(_labAccessAppId, _labAccessClientSecret).ConfigureAwait(false);

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", string.Format(CultureInfo.InvariantCulture, "bearer {0}", res));               
                return await httpClient.GetStringAsync(uriBuilder.ToString()).ConfigureAwait(false);               
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
            return CreateLabResponseFromResultString(result);
        }
    }
}
