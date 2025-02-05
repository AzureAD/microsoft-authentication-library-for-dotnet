// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.ManagedIdentity;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Internal.Utilities;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class CredentialManagedIdentityAuthRequest : ManagedIdentityAuthRequest
    {
        internal const string IdentityUnavailableError = "[Managed Identity] Authentication unavailable. " +
            "Either the requested identity has not been assigned to this resource, or other errors could " +
            "be present. Ensure the identity is correctly assigned and check the inner exception for more " +
            "details. For more information, visit https://aka.ms/msal-managed-identity.";

        internal const string GatewayError = "[Managed Identity] Authentication unavailable. " +
            "The request failed due to a gateway error.";

        private readonly Uri _credentialEndpoint = new Uri("http://169.254.169.254/metadata/identity/credential?cred-api-version=1.0");
        private readonly X509Certificate2 _certificate;

        public CredentialManagedIdentityAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
            _certificate = CertificateHelper.GetOrCreateCertificate();
        }

        protected override async Task<AuthenticationResult> GetAccessTokenAsync(
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            //calls sent to app token provider
            AuthenticationResult authResult = null;
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;

            //allow only one call to the provider 
            logger.Verbose(() => "[CredentialManagedIdentityAuthRequest] Entering acquire token for managed identity credential request semaphore.");
            await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[CredentialManagedIdentityAuthRequest] Entered acquire token for managed identity credential request semaphore.");

            try
            {
                // Bypass cache and send request to token endpoint, when 
                // 1. Force refresh is requested, or
                // 2. Claims are passed, or 
                // 3. If the AT needs to be refreshed pro-actively 
                if (_managedIdentityParameters.ForceRefresh ||
                    !string.IsNullOrEmpty(AuthenticationRequestParameters.Claims) ||
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ProactivelyRefreshed)
                {
                    authResult = await GetAccessTokenFromTokenEndpointAsync(cancellationToken, logger).ConfigureAwait(false);
                }
                else
                {
                    cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                    if (cachedAccessTokenItem == null)
                    {
                        authResult = await GetAccessTokenFromTokenEndpointAsync(cancellationToken, logger).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.Verbose(() => "[CredentialManagedIdentityAuthRequest] Getting Access token from cache ...");
                        authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                    }
                }

                return authResult;
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[CredentialManagedIdentityAuthRequest] Released acquire token for managed identity credential request semaphore.");
            }
        }

        private async Task<AuthenticationResult> GetAccessTokenFromTokenEndpointAsync(
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            string message;
            Exception exception = null;

            try
            {
                logger.Verbose(() => $"[CredentialManagedIdentityAuthRequest] Getting token from the managed identity endpoint using certificate: " +
                     $"Subject: {_certificate.Subject}, " +
                     $"Expiration: {_certificate.NotAfter}, " +
                     $"Thumbprint: {_certificate.Thumbprint}, " +
                     $"Issuer: {_certificate.Issuer}");

                ManagedIdentityCredentialResponse credentialResponse =
                    await GetCredentialAssertionAsync(_certificate, logger, cancellationToken).ConfigureAwait(false);

                var baseUri = new Uri(credentialResponse.RegionalTokenUrl);
                var tokenUrl = new Uri(baseUri, $"{credentialResponse.TenantId}/oauth2/v2.0/token");

                logger.Verbose(() => $"[CredentialManagedIdentityAuthRequest] Token endpoint : {tokenUrl}.");

                OAuth2Client client = CreateClientRequest(
                    AuthenticationRequestParameters.RequestContext.ServiceBundle.HttpManager,
                    credentialResponse);

                MsalTokenResponse msalTokenResponse = await client
                        .GetTokenAsync(tokenUrl,
                        AuthenticationRequestParameters.RequestContext,
                        true,
                        AuthenticationRequestParameters.OnBeforeTokenRequestHandler).ConfigureAwait(false);

                msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

                logger.Info("[CredentialManagedIdentityAuthRequest] Successful response received.");

                return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse)
                    .ConfigureAwait(false);
            }
            catch (MsalClientException ex)
            {
                logger.Verbose(() => $"[CredentialManagedIdentityAuthRequest] Caught an exception. {ex.Message}");
                throw;
            }
            catch (HttpRequestException ex)
            {
                exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                MsalError.ManagedIdentityRequestFailed,
                ex.Message,
                ex,
                ManagedIdentitySource.Credential,
                null);

                logger.Verbose(() => $"[CredentialManagedIdentityAuthRequest] Caught an exception. {ex.Message}");

                throw exception;
            }
            catch (MsalServiceException ex)
            {
                logger.Verbose(() => $"[CredentialManagedIdentityAuthRequest] Caught an exception. {ex.Message}. Error Code : {ex.ErrorCode} Status Code : {ex.StatusCode}");

                exception = MsalServiceExceptionFactory.CreateManagedIdentityException(
                    ex.ErrorCode,
                    ex.Message,
                    ex,
                    ManagedIdentitySource.Credential,
                    ex.StatusCode);

                throw exception;
            }
            catch (Exception e) when (e is not MsalServiceException)
            {
                logger.Error($"[CredentialManagedIdentityAuthRequest] Exception: {e.Message}");
                exception = e;
                message = MsalErrorMessage.CredentialEndpointNoResponseReceived;
            }

            MsalException msalException = MsalServiceExceptionFactory.CreateManagedIdentityException(
                MsalError.ManagedIdentityRequestFailed,
                message,
                exception,
                ManagedIdentitySource.Credential,
                null);

            throw msalException;
        }

        private async Task<ManagedIdentityCredentialResponse> GetCredentialAssertionAsync(
            X509Certificate2 credentialCertificate,
            ILoggerAdapter logger,
            CancellationToken cancellationToken
            )
        {
            // Create a Managed Identity request dynamically using the provided resource.
            string resource = GetOverriddenScopes(AuthenticationRequestParameters.Scope).AsSingleString();
            ManagedIdentityRequest miRequest = CreateManagedIdentityRequest(resource);

            var msiCredentialService = new ManagedIdentityCredentialService(
                miRequest,
                credentialCertificate,
                AuthenticationRequestParameters.RequestContext,
                cancellationToken);

            ManagedIdentityCredentialResponse credentialResponse = await msiCredentialService.GetCredentialAsync().ConfigureAwait(false);

            logger.Verbose(() => "[CredentialManagedIdentityAuthRequest] A credential was successfully fetched.");

            return credentialResponse;
        }

        /// <summary>
        /// Creates a Managed Identity request dynamically, including user-assigned identity if needed.
        /// </summary>
        private ManagedIdentityRequest CreateManagedIdentityRequest(string resource)
        {
            var request = new ManagedIdentityRequest(HttpMethod.Post, _credentialEndpoint);

            switch (AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    AuthenticationRequestParameters.RequestContext.Logger.Info("[Managed Identity] Adding user-assigned client ID to the request.");
                    request.QueryParameters[Constants.ManagedIdentityClientId] = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ResourceId:
                    AuthenticationRequestParameters.RequestContext.Logger.Info("[Managed Identity] Adding user-assigned resource ID to the request.");
                    request.QueryParameters[Constants.ManagedIdentityResourceId] = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;

                case AppConfig.ManagedIdentityIdType.ObjectId:
                    AuthenticationRequestParameters.RequestContext.Logger.Info("[Managed Identity] Adding user-assigned object ID to the request.");
                    request.QueryParameters[Constants.ManagedIdentityObjectId] = AuthenticationRequestParameters.RequestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId;
                    break;
            }

            return request;
        }

        /// <summary>
        /// Creates an OAuth2 client request for fetching the managed identity credential.
        /// </summary>
        /// <param name="httpManager"></param>
        /// <param name="credentialResponse"></param>
        /// <returns></returns>
        private OAuth2Client CreateClientRequest(
            IHttpManager httpManager,
            ManagedIdentityCredentialResponse credentialResponse)
        {
            // Initialize an OAuth2 client with logger, HTTP manager, and binding certificate.
            var client = new OAuth2Client(
                AuthenticationRequestParameters.RequestContext.Logger,
                httpManager,
                _certificate);

            // Convert overridden scopes to a single string.
            string scopes = GetOverriddenScopes(AuthenticationRequestParameters.Scope).AsSingleString();

            //credential flows must have a scope value with /.default suffixed to the resource identifier (application ID URI)
            scopes += "/.default";

            // Add required parameters for client credentials grant request.
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.ClientCredentials);
            client.AddBodyParameter(OAuth2Parameter.Scope, scopes);
            client.AddBodyParameter(OAuth2Parameter.ClientId, credentialResponse.ClientId);
            client.AddBodyParameter(OAuth2Parameter.ClientAssertion, credentialResponse.Credential);
            client.AddBodyParameter(OAuth2Parameter.ClientAssertionType, OAuth2AssertionType.JwtBearer);

            // Add optional claims and client capabilities parameter if provided.
            if (!string.IsNullOrWhiteSpace(AuthenticationRequestParameters.ClaimsAndClientCapabilities))
            {
                client.AddBodyParameter(OAuth2Parameter.Claims, AuthenticationRequestParameters.ClaimsAndClientCapabilities);
            }

            // Return the configured OAuth2 client.
            return client;
        }
    }
}
