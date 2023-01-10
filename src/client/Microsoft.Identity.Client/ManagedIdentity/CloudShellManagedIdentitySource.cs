// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net.Http;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/CloudShellManagedIdentitySource.cs
    /// </summary>
    internal class CloudShellManagedIdentitySource : ManagedIdentitySource
    {
        private readonly Uri _endpoint;

        public static ManagedIdentitySource TryCreate(RequestContext requestContext)
        {
            string msiEndpoint = EnvironmentVariables.MsiEndpoint;

            // if ONLY the env var MSI_ENDPOINT is set the MsiType is CloudShell
            if (string.IsNullOrEmpty(msiEndpoint))
            {
                return default;
            }

            Uri endpointUri;
            try
            {
                endpointUri = new Uri(msiEndpoint);
            }
            catch (FormatException ex)
            {
                throw new MsalClientException(MsalError.InvalidManagedIdentityEndpoint, string.Format(
                    CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "MSI_ENDPOINT", msiEndpoint, "Cloud Shell"), ex);
            }

            return new CloudShellManagedIdentitySource(endpointUri, requestContext);
        }

        private CloudShellManagedIdentitySource(Uri endpoint, RequestContext requestContext) : base(requestContext)
        {
            _endpoint = endpoint;
            if (!string.IsNullOrEmpty(requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId) || null != requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId)
            {
                throw new MsalClientException(MsalError.UserAssignedManagedIdentityNotSupported, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported);
            }
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new ManagedIdentityRequest(HttpMethod.Post, _endpoint);

            request.Headers.Add("ContentType", "application/x-www-form-urlencoded");
            request.Headers.Add("Metadata", "true");

            request.BodyParameters.Add("resource", resource);

            return request;
        }
    }
}
