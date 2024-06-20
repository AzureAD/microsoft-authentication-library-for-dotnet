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
    internal class ServiceFabricManagedIdentitySource : AbstractManagedIdentity
    {
        private const string ServiceFabricMsiApiVersion = "2019-07-01-preview";
        private readonly Uri _endpoint;
        private readonly string _identityHeaderValue;
        internal static Lazy<HttpClient> _httpClient = new Lazy<HttpClient>();

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;

            requestContext.Logger.Info(() => "[Managed Identity] Service fabric managed identity is available.");

            if (!Uri.TryCreate(identityEndpoint, UriKind.Absolute, out Uri endpointUri))
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError,
                        "IDENTITY_ENDPOINT", identityEndpoint, "Service Fabric");

                // Use the factory to create and throw the exception
                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    null, 
                    ManagedIdentitySource.ServiceFabric,
                    null); 

                throw exception;
            }

            requestContext.Logger.Verbose(() => "[Managed Identity] Creating Service Fabric managed identity. Endpoint URI: " + identityEndpoint);
            return new ServiceFabricManagedIdentitySource(requestContext, endpointUri, EnvironmentVariables.IdentityHeader);
        }

        internal override HttpClient CreateCustomHttpClient(RequestContext requestContext)
        {
            return new HttpClient(CreateHandlerWithSslValidation(requestContext.Logger));
        }

        internal HttpClientHandler CreateHandlerWithSslValidation(ILoggerAdapter logger)
        {
#if NET471_OR_GREATER || NETSTANDARD1_3_OR_GREATER || NET
            logger.Info(() => "[Managed Identity] Setting up server certificate validation callback.");
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
                    {
                        return 0 == string.Compare(certificate.Thumbprint, EnvironmentVariables.IdentityServerThumbprint, StringComparison.OrdinalIgnoreCase);
                    }
                    return true;
                }
            };
#else
            logger.Warning("[Managed Identity] Server certificate validation callback is not supported on .NET Framework.");
            return new HttpClientHandler();
#endif
        }


        private ServiceFabricManagedIdentitySource(RequestContext requestContext, Uri endpoint, string identityHeaderValue) : 
        base(requestContext, ManagedIdentitySource.ServiceFabric)
        {
            _endpoint = endpoint;
            _identityHeaderValue = identityHeaderValue;

            if (requestContext.ServiceBundle.Config.ManagedIdentityId.IsUserAssigned)
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
