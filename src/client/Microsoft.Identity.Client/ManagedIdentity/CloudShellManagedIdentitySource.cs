// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net.Http;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/CloudShellManagedIdentitySource.cs
    /// </summary>
    internal class CloudShellManagedIdentitySource : AbstractManagedIdentity
    {
        private readonly Uri _endpoint;
        private const string CloudShell = "Cloud Shell";

        public static AbstractManagedIdentity TryCreate(RequestContext requestContext)
        {
            string msiEndpoint = EnvironmentVariables.MsiEndpoint;

            // if ONLY the env var MSI_ENDPOINT is set the MsiType is CloudShell
            if (string.IsNullOrEmpty(msiEndpoint))
            {
                requestContext.Logger.Verbose(()=>"[Managed Identity] Cloud shell managed identity is unavailable.");
                return null;
            }

            Uri endpointUri;
            try
            {
                endpointUri = new Uri(msiEndpoint);
            }
            catch (FormatException ex)
            {
                requestContext.Logger.Error("[Managed Identity] Invalid endpoint found for the environment variable MSI_ENDPOINT: " + msiEndpoint);

                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    MsalErrorMessage.ManagedIdentityEndpointInvalidUriError,
                    "MSI_ENDPOINT", msiEndpoint, CloudShell);

                // Use the factory to create and throw the exception
                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    ex, 
                    ManagedIdentitySource.CloudShell,
                    null); 

                throw exception;
            }

            requestContext.Logger.Verbose(()=>"[Managed Identity] Creating cloud shell managed identity. Endpoint URI: " + msiEndpoint);
            return new CloudShellManagedIdentitySource(endpointUri, requestContext);
        }

        private CloudShellManagedIdentitySource(Uri endpoint, RequestContext requestContext) : 
            base(requestContext, ManagedIdentitySource.CloudShell)
        {
            _endpoint = endpoint;

            if (requestContext.ServiceBundle.Config.ManagedIdentityId.IsUserAssigned)
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture, 
                    MsalErrorMessage.ManagedIdentityUserAssignedNotSupported, 
                    CloudShell);

                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.UserAssignedManagedIdentityNotSupported,
                    errorMessage,
                    null,
                    ManagedIdentitySource.CloudShell,
                    null);

                throw exception;
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
