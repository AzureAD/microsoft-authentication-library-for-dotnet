// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class MachineLearningManagedIdentitySource : AbstractManagedIdentity
    {
        private const string MachineLearningMsiApiVersion = "2017-09-01";
        private const string SecretHeaderName = "secret";

        private readonly Uri _endpoint;
        private readonly string _secret;

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            requestContext.Logger.Info(() => "[Managed Identity] Machine learning managed identity is available.");

            return TryValidateEnvVars(EnvironmentVariables.MsiEndpoint, requestContext.Logger, out Uri endpointUri)
                ? new MachineLearningManagedIdentitySource(requestContext, endpointUri, EnvironmentVariables.MsiSecret)
                : null;
        }

        private MachineLearningManagedIdentitySource(RequestContext requestContext, Uri endpoint, string secret) 
            : base(requestContext, ManagedIdentitySource.MachineLearning)
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
                    "MSI_ENDPOINT", msiEndpoint, "Machine learning");

                // Use the factory to create and throw the exception
                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    ex, 
                    ManagedIdentitySource.MachineLearning,
                    null); // statusCode is null in this case

                throw exception;
            }

            logger.Info($"[Managed Identity] Environment variables validation passed for machine learning managed identity. Endpoint URI: {endpointUri}. Creating machine learning managed identity.");
            return true;
        }

        protected override ManagedIdentityRequest CreateRequest(string resource)
        {
            ManagedIdentityRequest request = new(System.Net.Http.HttpMethod.Get, _endpoint);

            request.Headers.Add("Metadata", "true");
            request.Headers.Add(SecretHeaderName, _secret);
            request.QueryParameters["api-version"] = MachineLearningMsiApiVersion;
            request.QueryParameters["resource"] = resource;

            switch (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    // Use the new 2017 constant for older ML-based environment
                    request.QueryParameters[Constants.ManagedIdentityClientId2017] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
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
