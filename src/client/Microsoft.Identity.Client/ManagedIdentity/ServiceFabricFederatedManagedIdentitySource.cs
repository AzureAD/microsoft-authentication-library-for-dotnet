// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ServiceFabricFederatedManagedIdentitySource : AbstractManagedIdentity
    {
        private string _serviceFabricMsiApiVersion = EnvironmentVariables.FmiServiceFabricApiVersion;
        private readonly Uri _endpoint;
        private readonly string _identityHeaderValue;
        private static string _mitsEndpointFmiPath => "/metadata/identity/oauth2/fmi/credential";

        internal static Lazy<HttpClient> _httpClientLazy;

        public static AbstractManagedIdentity Create(RequestContext requestContext)
        {
            VerifyEnvVariablesAreAvailable();

            Uri endpointUri;
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;

            requestContext.Logger.Info(() => "[Managed Identity] Service fabric federated managed identity is available.");
            identityEndpoint = EnvironmentVariables.FmiServiceFabricEndpoint;
            requestContext.Logger.Info(() => "[Managed Identity] Using FMI Service fabric endpoint.");

            if (!Uri.TryCreate(identityEndpoint + _mitsEndpointFmiPath, UriKind.Absolute, out endpointUri))
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityEndpointInvalidUriError,
                        "APP_IDENTITY_ENDPOINT", identityEndpoint, "FMI Service Fabric");

                // Use the factory to create and throw the exception
                var exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    MsalError.InvalidManagedIdentityEndpoint,
                    errorMessage,
                    null,
                    ManagedIdentitySource.ServiceFabricFederated,
                    null);

                throw exception;
            }

            requestContext.Logger.Verbose(() => "[Managed Identity] Creating Service Fabric federated managed identity. Endpoint URI: " + identityEndpoint);

            return new ServiceFabricFederatedManagedIdentitySource(requestContext, endpointUri, EnvironmentVariables.IdentityHeader);
        }

        private static void VerifyEnvVariablesAreAvailable()
        {
            if (string.IsNullOrEmpty(EnvironmentVariables.IdentityServerThumbprint))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityFmiInvalidEnvVariableError,
                         "IDENTITY_SERVER_THUMBPRINT"));
            }
            if (string.IsNullOrEmpty(EnvironmentVariables.FmiServiceFabricEndpoint))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityFmiInvalidEnvVariableError,
                         "APP_IDENTITY_ENDPOINT"));
            }
            if (string.IsNullOrEmpty(EnvironmentVariables.IdentityHeader))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityFmiInvalidEnvVariableError,
                         "IDENTITY_HEADER"));
            }
            if (string.IsNullOrEmpty(EnvironmentVariables.FmiServiceFabricApiVersion))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, MsalErrorMessage.ManagedIdentityFmiInvalidEnvVariableError,
                         "IDENTITY_API_VERSION", "FMI Service Fabric"));
            }
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

        private ServiceFabricFederatedManagedIdentitySource(RequestContext requestContext, Uri endpoint, string identityHeaderValue) :
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
            _requestContext.Logger.Info("[Managed Identity] Request is for FMI, no ids or resource will be added to the request.");
            request.QueryParameters["api-version"] = _serviceFabricMsiApiVersion;
            return request;
        }

        internal string GetEndpointForTesting()
        {
            return _endpoint.ToString();
        }
    }
}
