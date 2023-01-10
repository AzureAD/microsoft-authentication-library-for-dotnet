// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net.Http;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ServiceFabricManagedIdentitySource : ManagedIdentitySource
    {
        private const string ServiceFabricMsiApiVersion = "2019-07-01-preview";

        private readonly Uri _endpoint;
        private readonly string _identityHeaderValue;

        public ServiceFabricManagedIdentitySource(RequestContext requestContext) : base(requestContext)
        {
        }

        public static ManagedIdentitySource TryCreate(RequestContext requestContext)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string identityHeader = EnvironmentVariables.IdentityHeader;
            string identityServerThumbprint = EnvironmentVariables.IdentityServerThumbprint;

            if (string.IsNullOrEmpty(identityEndpoint) || string.IsNullOrEmpty(identityHeader) || string.IsNullOrEmpty(identityServerThumbprint))
            {
                requestContext.Logger.Verbose("[Managed Identity] Service Fabric managed identity unavailable.");
                return null;
            }

            if (!Uri.TryCreate(identityEndpoint, UriKind.Absolute, out Uri endpointUri))
            {
                throw new MsalClientException(MsalError.AuthenticationFailed, string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, identityEndpoint, "Service fabric"));
            }

            requestContext.Logger.Verbose("[Managed Identity] Creating service fabric managed identity. Endpoint URI: " + identityEndpoint);
            return new ServiceFabricManagedIdentitySource(requestContext, endpointUri, identityHeader);
        }

        private ServiceFabricManagedIdentitySource(RequestContext requestContext, Uri endpoint, string identityHeaderValue) : base(requestContext)
        {
            _endpoint = endpoint;
            _identityHeaderValue = identityHeaderValue;

            if (!string.IsNullOrEmpty(requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId) || 
                !string.IsNullOrEmpty(requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId))
            {
                throw new MsalClientException(MsalError.UserAssignedManagedIdentityNotSupported, MsalErrorMessage.ManagedIdentityUserAssignedNotSupported);
            }
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new ManagedIdentityRequest(HttpMethod.Get, _endpoint);

            request.Headers["secret"] = _identityHeaderValue;

            request.QueryParameters["api-version"] = ServiceFabricMsiApiVersion;
            request.QueryParameters["resource"] = resource;

            return request;
        }
    }
}
