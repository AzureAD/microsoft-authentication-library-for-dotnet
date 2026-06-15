// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ServiceFabricManagedIdentitySource : AbstractManagedIdentity
    {
        private const string ServiceFabricMsiApiVersion = "2020-05-01";
        private readonly Uri _endpoint;
        private readonly string _identityHeaderValue;

        internal static Lazy<HttpClient> _httpClientLazy;

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

        internal override Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> GetValidationCallback()
        {
            return ValidateServerCertificateCallback;
        }

        private bool ValidateServerCertificateCallback(HttpRequestMessage message, X509Certificate2 certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            return string.Equals(certificate.GetCertHashString(), EnvironmentVariables.IdentityServerThumbprint, StringComparison.OrdinalIgnoreCase);
        }

        private ServiceFabricManagedIdentitySource(RequestContext requestContext, Uri endpoint, string identityHeaderValue) : 
        base(requestContext, ManagedIdentitySource.ServiceFabric)
        {
            _endpoint = endpoint;
            _identityHeaderValue = identityHeaderValue;

            if (requestContext.ServiceBundle.Config.ManagedIdentityId.IsUserAssigned)
            {
                var idType = requestContext.ServiceBundle.Config.ManagedIdentityId.IdType;

                // Service Fabric only supports object ID (principalId) for user-assigned managed identity.
                // ClientId and ResourceId are not supported today. When SF adds support for them,
                // remove or update this check and add the corresponding cases in CreateRequestAsync.
                if (idType != AppConfig.ManagedIdentityIdType.ObjectId)
                {
                    throw MsalServiceExceptionFactory.CreateManagedIdentityException(
                        MsalError.UserAssignedManagedIdentityNotConfigurableAtRuntime,
                        MsalErrorMessage.ManagedIdentityUserAssignedNotConfigurableAtRuntime,
                        null,
                        ManagedIdentitySource.ServiceFabric,
                        null);
                }
            }
        }

        protected override Task<ManagedIdentityRequest> CreateRequestAsync(string resource)
        {
            ManagedIdentityRequest request = new ManagedIdentityRequest(HttpMethod.Get, _endpoint);

            request.Headers["secret"] = _identityHeaderValue;

            request.QueryParameters["api-version"] = ServiceFabricMsiApiVersion;
            request.QueryParameters["resource"] = resource;

            // Service Fabric only supports object ID (sent as 'principalId'). The constructor
            // rejects ClientId/ResourceId for user-assigned identities, so only ObjectId can
            // reach this point.
            if (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType == AppConfig.ManagedIdentityIdType.ObjectId)
            {
                _requestContext.Logger.Info("[Managed Identity] Adding user assigned object id as principalId to the request.");
                request.QueryParameters[Constants.ServiceFabricManagedIdentityPrincipalId] = _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
            }

            return Task.FromResult(request);
        }
    }
}
