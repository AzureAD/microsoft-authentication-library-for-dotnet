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

        public static ManagedIdentitySource TryCreate(RequestContext requestContext)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string identityHeader = EnvironmentVariables.IdentityHeader;
            string identityServerThumbprint = EnvironmentVariables.IdentityServerThumbprint;

            if (string.IsNullOrEmpty(identityEndpoint) || string.IsNullOrEmpty(identityHeader) || string.IsNullOrEmpty(identityServerThumbprint))
            {
                requestContext.Logger.Verbose(() => "[Managed Identity] Service Fabric managed identity unavailable.");
                return null;
            }

            if (!Uri.TryCreate(identityEndpoint, UriKind.Absolute, out Uri endpointUri))
            {
                throw new MsalClientException(MsalError.InvalidManagedIdentityEndpoint, string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError, "IDENTITY_ENDPOINT", identityEndpoint, "Service Fabric"));
            }

            requestContext.Logger.Verbose(() => "[Managed Identity] Creating Service Fabric managed identity. Endpoint URI: " + identityEndpoint);
            return new ServiceFabricManagedIdentitySource(requestContext, endpointUri, identityHeader);
        }

        private ServiceFabricManagedIdentitySource(RequestContext requestContext, Uri endpoint, string identityHeaderValue) : base(requestContext)
        {
            _endpoint = endpoint;
            _identityHeaderValue = identityHeaderValue;

            if (!string.IsNullOrEmpty(requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId) || 
                !string.IsNullOrEmpty(requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId))
            {
                requestContext.Logger.Warning(MsalErrorMessage.ManagedIdentityUserAssignedNotConfigurableAtRuntime);
            }
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new ManagedIdentityRequest(HttpMethod.Get, _endpoint);

            request.Headers["secret"] = _identityHeaderValue;

            request.QueryParameters["api-version"] = ServiceFabricMsiApiVersion;
            request.QueryParameters["resource"] = resource;



            if (!string.IsNullOrEmpty(_requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId))
            {
                _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                request.QueryParameters[Constants.ManagedIdentityClientId] = _requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedClientId;
            }

            if (!string.IsNullOrEmpty(_requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId))
            {
                _requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                request.QueryParameters[Constants.ManagedIdentityResourceId] = _requestContext.ServiceBundle.Config.ManagedIdentityUserAssignedResourceId;
            }

            return request;
        }
    }
}
