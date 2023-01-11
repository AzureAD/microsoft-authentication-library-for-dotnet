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
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/ServiceFabricManagedIdentitySource.cs
    /// </summary>
    internal class ServiceFabricManagedIdentitySource : ManagedIdentitySource
    {
        private const string ServiceFabricMsiApiVersion = "2019-07-01-preview";

        private readonly Uri _endpoint;
        private readonly string _identityHeaderValue;
        private readonly string _clientId;
        private readonly string _resourceId;

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
                throw new MsalClientException(MsalError.InvalidManagedIdentityEndpoint, string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", identityEndpoint, "Service Fabric"));
            }

            requestContext.Logger.Verbose("[Managed Identity] Creating Service Fabric managed identity. Endpoint URI: " + identityEndpoint);
            return new ServiceFabricManagedIdentitySource(requestContext, endpointUri, identityHeader);
        }

        private ServiceFabricManagedIdentitySource(RequestContext requestContext, Uri endpoint, string identityHeaderValue) : base(requestContext)
        {
            _endpoint = endpoint;
            _identityHeaderValue = identityHeaderValue;
            _clientId = requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId;
            _resourceId = requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId;

            if (!string.IsNullOrEmpty(requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId) || 
                !string.IsNullOrEmpty(requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId))
            {
                throw new MsalClientException(MsalError.UserAssignedManagedIdentityNotConfigurableAtRuntime, MsalErrorMessage.ManagedIdentityUserAssignedNotConfigurableAtRuntime);
            }
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new ManagedIdentityRequest(HttpMethod.Get, _endpoint);

            request.Headers["secret"] = _identityHeaderValue;

            request.QueryParameters["api-version"] = ServiceFabricMsiApiVersion;
            request.QueryParameters["resource"] = resource;

            if (!string.IsNullOrEmpty(_clientId))
            {
                request.QueryParameters[Constants.ManagedIdentityClientId] = _clientId;
            }
            if (!string.IsNullOrEmpty(_resourceId))
            {
                request.QueryParameters[Constants.ManagedIdentityResourceId] = _resourceId;
            }

            return request;
        }
    }
}
