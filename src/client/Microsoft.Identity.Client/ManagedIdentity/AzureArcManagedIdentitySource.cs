// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class AzureArcManagedIdentitySource : ManagedIdentitySource
    {
        private const string IdentityEndpointInvalidUriError = "[Managed Identity] The environment variable IDENTITY_ENDPOINT contains an invalid Uri.";
        private const string NoChallengeErrorMessage = "[Managed Identity] Did not receive expected WWW-Authenticate header in the response from Azure Arc Managed Identity Endpoint.";
        private const string InvalidChallangeErrorMessage = "[Managed Identity] The WWW-Authenticate header in the response from Azure Arc Managed Identity Endpoint did not match the expected format.";
        private const string UserAssignedNotSupportedErrorMessage = "[Managed Identity] User assigned identity is not supported by the Azure Arc Managed Identity Endpoint. To authenticate with the system assigned identity omit the client id when constructing the ManagedIdentityCredential, or if authenticating with the DefaultAzureCredential ensure the AZURE_CLIENT_ID environment variable is not set.";
        private const string ArcApiVersion = "2019-11-01";

        private readonly string _clientId;
        private readonly string _resourceId;
        private readonly Uri _endpoint;

        public static ManagedIdentitySource TryCreate(RequestContext requestContext)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;

            // if BOTH the env vars IDENTITY_ENDPOINT and IMDS_ENDPOINT are set the MsiType is Azure Arc
            if (string.IsNullOrEmpty(identityEndpoint) || string.IsNullOrEmpty(imdsEndpoint))
            {
                return default;
            }

            if (!Uri.TryCreate(identityEndpoint, UriKind.Absolute, out Uri endpointUri))
            {
                throw new MsalClientException(MsalError.InvalidManagedIdentityEndpoint, IdentityEndpointInvalidUriError);
            }

            return new AzureArcManagedIdentitySource(endpointUri, requestContext);
        }

        private AzureArcManagedIdentitySource(Uri endpoint, RequestContext requestContext) : base(requestContext)
        {
            _endpoint = endpoint;
            _clientId = requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId;
            _resourceId = requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId;

            if (!string.IsNullOrEmpty(_clientId) || !string.IsNullOrEmpty(_resourceId))
            {
                throw new MsalClientException(MsalError.UserAssignedManagedIdentityNotSupported, UserAssignedNotSupportedErrorMessage);
            }
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new ManagedIdentityRequest(System.Net.Http.HttpMethod.Get, _endpoint);

            request.Headers.Add("Metadata", "true");
            request.QueryParameters["api-version"] = ArcApiVersion;
            request.QueryParameters["resource"] = resource;

            return request;
        }

        protected override async Task<ManagedIdentityResponse> HandleResponseAsync(
            AppTokenProviderParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (!response.HeadersAsDictionary.TryGetValue("WWW-Authenticate", out string challenge))
                {
                    throw new MsalServiceException(MsalError.ManagedIdentityRequestFailed, NoChallengeErrorMessage);
                }

                var splitChallenge = challenge.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                if (splitChallenge.Length != 2)
                {
                    throw new MsalClientException(MsalError.ManagedIdentityRequestFailed, InvalidChallangeErrorMessage);
                }

                var authHeaderValue = "Basic " + File.ReadAllText(splitChallenge[1]);

                ManagedIdentityRequest request = CreateRequest(ScopeHelper.ScopesToResource(parameters.Scopes.ToArray()));

                request.Headers.Add("Authorization", authHeaderValue);

                response = await _requestContext.ServiceBundle.HttpManager.SendGetAsync(request.ComputeUri(), request.Headers, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);

                return await base.HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
            }

            return await base.HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
        }
    }
}
