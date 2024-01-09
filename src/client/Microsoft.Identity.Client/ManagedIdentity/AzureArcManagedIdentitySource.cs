// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/AzureArcManagedIdentitySource.cs
    /// </summary>
    internal class AzureArcManagedIdentitySource : AbstractManagedIdentity
    {
        private const string ArcApiVersion = "2019-11-01";
        private const string AzureArc = "Azure Arc";

        private readonly Uri _endpoint;

        public static AbstractManagedIdentity TryCreate(RequestContext requestContext)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;

            // if BOTH the env vars IDENTITY_ENDPOINT and IMDS_ENDPOINT are set the MsiType is Azure Arc
            if (string.IsNullOrEmpty(identityEndpoint) || string.IsNullOrEmpty(imdsEndpoint))
            {
                requestContext.Logger.Verbose(()=>"[Managed Identity] Azure Arc managed identity is unavailable.");
                return null;
            }

            if (!Uri.TryCreate(identityEndpoint, UriKind.Absolute, out Uri endpointUri))
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    MsalErrorMessage.ManagedIdentityEndpointInvalidUriError,
                    "IDENTITY_ENDPOINT", identityEndpoint, AzureArc);

                // Use the factory to create and throw the exception
                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    null, 
                    ManagedIdentitySource.AzureArc,
                    null); 

                throw exception;
            }

            requestContext.Logger.Verbose(()=>"[Managed Identity] Creating Azure Arc managed identity. Endpoint URI: " + endpointUri);
            return new AzureArcManagedIdentitySource(endpointUri, requestContext);
        }

        private AzureArcManagedIdentitySource(Uri endpoint, RequestContext requestContext) : 
            base(requestContext, ManagedIdentitySource.AzureArc)
        {
            _endpoint = endpoint;

            if (requestContext.ServiceBundle.Config.ManagedIdentityId.IsUserAssigned)
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported, AzureArc);

                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.UserAssignedManagedIdentityNotSupported, 
                    errorMessage, 
                    null, 
                    ManagedIdentitySource.AzureArc, 
                    null);

                throw exception;
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
            AcquireTokenForManagedIdentityParameters parameters,
            HttpResponse response,
            CancellationToken cancellationToken)
        {
            _requestContext.Logger.Verbose(() => $"[Managed Identity] Response received. Status code: {response.StatusCode}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (!response.HeadersAsDictionary.TryGetValue("WWW-Authenticate", out string challenge))
                {
                    _requestContext.Logger.Error("[Managed Identity] WWW-Authenticate header is expected but not found.");

                    var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.ManagedIdentityRequestFailed,
                        MsalErrorMessage.ManagedIdentityNoChallengeError,
                        null,
                        ManagedIdentitySource.AzureArc,
                        null);

                    throw exception;
                }

                var splitChallenge = challenge.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                if (splitChallenge.Length != 2)
                {
                    _requestContext.Logger.Error("[Managed Identity] The WWW-Authenticate header for Azure arc managed identity is not an expected format.");

                    var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.ManagedIdentityRequestFailed,
                        MsalErrorMessage.ManagedIdentityInvalidChallenge,
                        null,
                        ManagedIdentitySource.AzureArc,
                        null);

                    throw exception;
                }

                var authHeaderValue = "Basic " + File.ReadAllText(splitChallenge[1]);

                ManagedIdentityRequest request = CreateRequest(parameters.Resource);

                _requestContext.Logger.Verbose(() => "[Managed Identity] Adding authorization header to the request.");
                request.Headers.Add("Authorization", authHeaderValue);

                response = await _requestContext.ServiceBundle.HttpManager.SendGetAsync(request.ComputeUri(), request.Headers, _requestContext.Logger, cancellationToken: cancellationToken).ConfigureAwait(false);

                return await base.HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
            }

            return await base.HandleResponseAsync(parameters, response, cancellationToken).ConfigureAwait(false);
        }
    }
}
