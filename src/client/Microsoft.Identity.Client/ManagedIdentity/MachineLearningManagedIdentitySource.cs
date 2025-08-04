// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class MachineLearningManagedIdentitySource : AbstractManagedIdentity
    {
        private const string MachineLearning = "Machine Learning";

        private const string MachineLearningMsiApiVersion = "2017-09-01";
        private const string SecretHeaderName = "secret";

        private readonly Uri _endpoint;
        private readonly string _secret;

        public const string UnsupportedIdTypeError = "Only client id is supported for user-assigned managed identity in Machine Learning."; // referenced in unit test

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

                throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    ex, 
                    ManagedIdentitySource.MachineLearning,
                    null); // statusCode is null in this case
            }

            logger.Info($"[Managed Identity] Environment variables validation passed for machine learning managed identity. Endpoint URI: {endpointUri}. Creating machine learning managed identity.");
            return true;
        }

        protected override ManagedIdentityRequest CreateRequest(string resource, 
            AcquireTokenForManagedIdentityParameters parameters)
        {
            ManagedIdentityRequest request = new(System.Net.Http.HttpMethod.Get, _endpoint);

            request.Headers.Add("Metadata", "true");
            request.Headers.Add(SecretHeaderName, _secret);
            request.QueryParameters["api-version"] = MachineLearningMsiApiVersion;
            request.QueryParameters["resource"] = resource;

            switch (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.SystemAssigned:
                    _requestContext.Logger.Info("[Managed Identity] Adding system assigned client id to the request.");

                    // this environment variable is always set in an Azure Machine Learning source, but check if null just in case
                    if (EnvironmentVariables.MachineLearningDefaultClientId == null)
                    {
                        throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                            MsalError.InvalidManagedIdentityIdType,
                            "The DEFAULT_IDENTITY_CLIENT_ID environment variable is null.",
                            null, // configuration error
                            ManagedIdentitySource.MachineLearning,
                            null); // statusCode is null in this case
                    }

                    // Use the new 2017 constant for older ML-based environment
                    request.QueryParameters[Constants.ManagedIdentityClientId2017] = EnvironmentVariables.MachineLearningDefaultClientId;
                    break;

                case AppConfig.ManagedIdentityIdType.ClientId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    // Use the new 2017 constant for older ML-based environment
                    request.QueryParameters[Constants.ManagedIdentityClientId2017] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                default:
                    throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.InvalidManagedIdentityIdType,
                        UnsupportedIdTypeError,
                        null, // configuration error
                        ManagedIdentitySource.MachineLearning,
                        null); // statusCode is null in this case
            }
                
            return request;
        }
    }
}
