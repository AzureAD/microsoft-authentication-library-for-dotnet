﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class AppServiceManagedIdentitySource : AbstractManagedIdentity
    {
        // MSI Constants. Docs for MSI are available here https://docs.microsoft.com/azure/app-service/overview-managed-identity
        private const string AppServiceMsiApiVersion = "2019-08-01";
        private const string SecretHeaderName = "X-IDENTITY-HEADER";

        private readonly Uri _endpoint;
        private readonly string _secret;

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            requestContext.Logger.Info(() => "[Managed Identity] App service managed identity is available.");

            return TryValidateEnvVars(EnvironmentVariables.IdentityEndpoint, requestContext.Logger, out Uri endpointUri)
                ? new AppServiceManagedIdentitySource(requestContext, endpointUri, EnvironmentVariables.IdentityHeader)
                : null;
        }

        private AppServiceManagedIdentitySource(RequestContext requestContext, Uri endpoint, string secret) 
            : base(requestContext, ManagedIdentitySource.AppService)
        {
            _endpoint = endpoint;
            _secret = secret;
        }

        private static bool TryValidateEnvVars(string msiEndpoint, ILoggerAdapter logger, out Uri endpointUri)
        {
            endpointUri = null;

            try
            {
                endpointUri = new Uri(msiEndpoint);
            }
            catch (FormatException ex)
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    MsalErrorMessage.ManagedIdentityEndpointInvalidUriError,
                    "IDENTITY_ENDPOINT", msiEndpoint, "App Service");

                // Use the factory to create and throw the exception
                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    ex, 
                    ManagedIdentitySource.AppService,
                    null); // statusCode is null in this case

                throw exception;
            }

            logger.Info($"[Managed Identity] Environment variables validation passed for app service managed identity. Endpoint URI: {endpointUri}. Creating App Service managed identity.");
            return true;
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new(System.Net.Http.HttpMethod.Get, _endpoint);
            
            request.Headers.Add(SecretHeaderName, _secret);
            request.QueryParameters["api-version"] = AppServiceMsiApiVersion;
            request.QueryParameters["resource"] = resource;

            switch (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityClientId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ResourceId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityResourceId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ObjectId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned object id to the request.");
                    request.QueryParameters[Constants.ManagedIdentityObjectId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;
            }
                
            return request;
        }
    }
}
