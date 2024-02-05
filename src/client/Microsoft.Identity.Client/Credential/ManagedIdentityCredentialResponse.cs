// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Identity.Client.Credential
{
    internal class ManagedIdentityCredentialResponse : IManagedIdentityCredentialResponse
    {
        private readonly Uri _uri;
        private readonly X509Certificate2 _bindingCertificate;
        private readonly RequestContext _requestContext;
        private readonly CancellationToken _cancellationToken;
        internal const string TimeoutError = "[Managed Identity] Authentication unavailable. The request to the managed identity endpoint timed out.";

        public ManagedIdentityCredentialResponse(
            Uri uri,
            X509Certificate2 bindingCertificate,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            _uri = uri;
            _bindingCertificate = bindingCertificate;
            _requestContext = requestContext;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets or fetches the Managed Identity credential from the cache or the service.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MsalManagedIdentityException"></exception>
        public async Task<CredentialResponse> GetCredentialAsync()
        {
            CredentialResponse credentialResponse = await FetchFromServiceAsync(
                _requestContext.ServiceBundle.HttpManager,
                _cancellationToken
            ).ConfigureAwait(false);

            ValidateCredentialResponse(credentialResponse);

            return credentialResponse;
        }

        /// <summary>
        /// Validates the properties of a CredentialResponse to ensure it meets the necessary criteria for authentication.
        /// </summary>
        /// <param name="credentialResponse">The CredentialResponse to be validated.</param>
        private void ValidateCredentialResponse(CredentialResponse credentialResponse)
        {
            var errorMessages = new List<string>();

            // Check if the CredentialResponse is null or if any required property is missing or invalid
            if (credentialResponse == null)
            {
                errorMessages.Add("CredentialResponse is null. ");
            }
            else
            {
                if (credentialResponse.Credential.IsNullOrEmpty())
                {
                    errorMessages.Add("Credential is missing or empty. ");
                }

                if (credentialResponse.RegionalTokenUrl.IsNullOrEmpty())
                {
                    errorMessages.Add("RegionalTokenUrl is missing or empty. ");
                }

                if (credentialResponse.ClientId.IsNullOrEmpty())
                {
                    errorMessages.Add("ClientId is missing or empty. ");
                }

                if (credentialResponse.TenantId.IsNullOrEmpty())
                {
                    errorMessages.Add("TenantId is missing or empty. ");
                }
            }

            // Check if any error messages were added
            if (errorMessages.Any())
            {
                // Log an error message indicating the missing or insufficient fields
                _requestContext.Logger.Error("[Managed Identity] " + string.Join(" ", errorMessages) +
                    " and/or insufficient for authentication.");

                // Throw an exception indicating that the CredentialResponse is invalid
                MsalException exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidResponse,
                    null,
                    ManagedIdentitySource.Credential,
                    null);

                throw exception;
            }
        }

        /// <summary>
        /// Fetches a new managed identity credential from the IMDS endpoint.
        /// </summary>
        /// <param name="httpManager"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="MsalManagedIdentityException"></exception>
        private async Task<CredentialResponse> FetchFromServiceAsync(
            IHttpManager httpManager,
            CancellationToken cancellationToken)
        {
            _requestContext.Logger.Info("[Managed Identity] Fetching new managed identity credential from IMDS endpoint.");

            OAuth2Client client = CreateClientRequest(httpManager);

            CredentialResponse credentialResponse = await client
                .GetCredentialResponseAsync(_uri, _requestContext)
                .ConfigureAwait(false);

            return credentialResponse;
        }

        /// <summary>
        /// Creates an OAuth2 client request for fetching the managed identity credential.
        /// </summary>
        /// <param name="httpManager"></param>
        /// <returns></returns>
        private OAuth2Client CreateClientRequest(IHttpManager httpManager)
        {
            var client = new OAuth2Client(_requestContext.Logger, httpManager, null);

            client.AddHeader("Metadata", "true");
            client.AddHeader("x-ms-client-request-id", _requestContext.CorrelationId.ToString("D"));
            string jsonPayload = GetCredentialPayload(_bindingCertificate);
            client.AddBodyContent(new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json"));

            return client;
        }

        /// <summary>
        /// Creates the payload for the managed identity credential request.
        /// </summary>
        /// <param name="x509Certificate2"></param>
        /// <returns></returns>
        private static string GetCredentialPayload(X509Certificate2 x509Certificate2)
        {
            string certificateBase64 = Convert.ToBase64String(x509Certificate2.Export(X509ContentType.Cert));

            return @"
                    {
                        ""cnf"": {
                            ""jwk"": {
                                ""kty"": ""RSA"", 
                                ""use"": ""sig"",
                                ""alg"": ""RS256"",
                                ""kid"": """ + x509Certificate2.Thumbprint + @""",
                                ""x5c"": [""" + certificateBase64 + @"""]
                            }
                        },
                        ""latch_key"": false    
                    }";
        }
    }
}
