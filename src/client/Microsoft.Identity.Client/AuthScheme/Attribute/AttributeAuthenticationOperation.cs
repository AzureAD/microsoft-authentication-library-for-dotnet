// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.AuthScheme.Attribute
{
    internal class AttributeAuthenticationOperation : IAuthenticationOperation
    {
        const string BearerTokenType = "bearer";

        private readonly string _fmiCredential;
        private readonly string _fmiPath;
        private readonly IServiceBundle _serviceBundle;

        public int TelemetryTokenType => TelemetryTokenTypeConstants.Bearer;

        public string AuthorizationHeaderPrefix => "Bearer";

        public string KeyId => "SomeKeyId";

        public string AccessTokenType => BearerTokenType;

        public AttributeAuthenticationOperation(IServiceBundle serviceBundle, string fmiCredential, string fmiPath)
        {
            _serviceBundle = serviceBundle;
            _fmiCredential = fmiCredential ?? throw new ArgumentNullException(nameof(fmiCredential), "Fmi Credential cannot be null");
            _fmiPath = fmiPath ?? throw new ArgumentNullException(nameof(fmiPath), "Fmi Path cannot be null");
        }

        //No operation needed for formatting the result in this case. Mise will combine this with the authZ token.
        public void FormatResult(AuthenticationResult authenticationResult)
        {
            //No Op
        }

        /// <summary>
        /// Need to acquire attribute token for the Attribute service.
        /// Then the attribute token will be provided as a parameter to the AuthN request.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            //Acquire token for Attribute service using MSAL
            //Can this be acquired from MISE since it is already acquiring this token?
            string authToken = GetAttributeAuthTokenAsync().GetAwaiter().GetResult();

            //Contact Attribute service to get the attribute token
            string attributeToken = GetAttributeTokenAsync(authToken, "attributes").GetAwaiter().GetResult();

            //Provide Attribute token to AuthN Request.
            return new Dictionary<string, string>() {
                { "attributeToken", attributeToken}
            };
        }

        public async Task<string> GetAttributeAuthTokenAsync()
        {
            string scope = "api://AzureFMITokenExchange/.default";

            //Create application
            var confidentialApp = ConfidentialClientApplicationBuilder
                        .Create("urn:microsoft:identity:fmi")
                        .WithAuthority("https://login.microsoftonline.com/", "f645ad92-e38d-4d1a-b510-d1b09a74a8ca")
                        .WithClientAssertion(() => { return _fmiCredential; })
                        .BuildConcrete();

            //Acquire attribute token
            var authResult = await confidentialApp.AcquireTokenForClient(new[] { scope })
                                                    .WithFmiPath(_fmiPath)
                                                    .WithExtraQueryParameters("dc=ESTS-PUB-AUC-FD000-TEST1-100")
                                                    .ExecuteAsync()
                                                    .ConfigureAwait(false);

            return authResult.AccessToken;
        }

        public async Task<string> GetAttributeTokenAsync(string authToken, string attributes)
        {
            var endpoint = new Uri("https://eastus2euap.authorization.azure.net/common/authorizationToken");

            // Prepare headers including the auth token
            var headers = new Dictionary<string, string>
                            {
                                { "Authorization", $"Bearer {authToken}" },
                                { "Content-Type", "application/json" }
                            };

            // Create request body with attributes
            var bodyContent = new StringContent(
                $"{{\"attributes\":\"{attributes}\"}}",
                System.Text.Encoding.UTF8,
                "application/json");

            // Get HttpManager instance (assuming it's available via DI or similar)
            var httpManager = _serviceBundle.HttpManager;
            IRetryPolicy policy = _serviceBundle.Config.RetryPolicyFactory.GetRetryPolicy(RequestType.STS);

            var response = await httpManager.SendRequestAsync(
                endpoint,
                headers,
                bodyContent,
                HttpMethod.Post,
                logger: _serviceBundle.ApplicationLogger,
                doNotThrow: false,
                mtlsCertificate: null,
                validateServerCertificate: null,
                cancellationToken: default,
                retryPolicy: policy)
                .ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                //Cache the attribute token in the Hybrid cache
                return response.Body;
            }

            throw new MsalServiceException(
                MsalError.AttributeTokenRequestFailed,
                $"Failed to get attribute token. Status code: {response.StatusCode}");
        }
    }
}
