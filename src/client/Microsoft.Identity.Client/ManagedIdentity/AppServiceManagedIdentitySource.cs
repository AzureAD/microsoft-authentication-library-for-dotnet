// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/AppServiceManagedIdentitySource.cs
    /// </summary>
    internal class AppServiceManagedIdentitySource : ManagedIdentitySource
    {
        // MSI Constants. Docs for MSI are available here https://docs.microsoft.com/azure/app-service/overview-managed-identity
        private const string AppServiceMsiApiVersion = "2019-08-01";
        private const string SecretHeaderName = "X-IDENTITY-HEADER";
        private const string ClientIdHeaderName = "client_id";

        private const string MsiEndpointInvalidUriError = "[Managed Identity App Service] The environment variable IDENTITY_ENDPOINT contains an invalid Uri {0}.";

        private readonly Uri _endpoint;
        private readonly string _secret;
        private readonly string _clientId;
        private readonly string _resourceId;

        public static ManagedIdentitySource TryCreate(RequestContext requestContext)
        {
            var msiSecret = EnvironmentVariables.IdentityHeader;
            
            if (TryValidateEnvVars(EnvironmentVariables.IdentityEndpoint, msiSecret, requestContext.Logger, out Uri endpointUri))
            {
                ManagedIdentitySourceName = "Managed Identity App Service";
                return new AppServiceManagedIdentitySource(requestContext, endpointUri, msiSecret);
            }

            return null;
        }

        private AppServiceManagedIdentitySource(RequestContext requestContext, Uri endpoint, string secret) : base(requestContext)
        {
            _endpoint = endpoint;
            _secret = secret;
            _clientId = requestContext.ServiceBundle.Config.MIUserAssignedClientId;
            _resourceId = requestContext.ServiceBundle.Config.MIUserAssignedResourceId;
        }

        private static bool TryValidateEnvVars(string msiEndpoint, string secret, ILoggerAdapter logger, out Uri endpointUri)
        {
            endpointUri = null;

            // if BOTH the env vars endpoint and secret values are null, this MSI provider is unavailable.
            if (string.IsNullOrEmpty(msiEndpoint) || string.IsNullOrEmpty(secret))
            {
                logger.Info($"[{ManagedIdentitySourceName}] App service managed identity is unavailable.");
                return false;
            }

            try
            {
                endpointUri = new Uri(msiEndpoint);
            }
            catch (FormatException ex)
            {
                throw new MsalClientException(MsalError.InvalidManagedIdentityEndpoint, string.Format(CultureInfo.InvariantCulture, MsiEndpointInvalidUriError, msiEndpoint), ex);
            }

            logger.Info($"[{ManagedIdentitySourceName}] Environment variables validation passed for app service managed identity. Endpoint uri: {endpointUri}");
            return true;
        }

        protected override ManagedIdentityRequest CreateRequest(string[] scopes)
        {
            // convert the scopes to a resource string
            string resource = ScopeHelper.ScopesToResource(scopes);

            ManagedIdentityRequest request = new ManagedIdentityRequest(System.Net.Http.HttpMethod.Get, _endpoint);
            
            request.Headers.Add(SecretHeaderName, _secret);
            request.QueryParameters["api-version"] = AppServiceMsiApiVersion;
            request.QueryParameters["resource"] = resource;

            if (!string.IsNullOrEmpty(_clientId))
            {
                _requestContext.Logger.Info($"[{ManagedIdentitySourceName}] Adding user assigned client id to the request.");
                request.QueryParameters[ClientIdHeaderName] = _clientId;
            }

            if (!string.IsNullOrEmpty(_resourceId))
            {
                _requestContext.Logger.Info($"[{ManagedIdentitySourceName}] Adding user assigned resource id to the request.");
                request.QueryParameters[Constants.ManagedIdentityResourceId] = _resourceId;
            }

            request.ComputeUri();
            return request;
        }
    }
}
