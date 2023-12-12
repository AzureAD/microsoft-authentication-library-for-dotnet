// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Credential;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class CredentialBasedMsiAuthRequest : RequestBase
    {
        private readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        private static readonly SemaphoreSlim s_semaphoreSlim = new(1, 1);
        private readonly Uri _credentialEndpoint;

        private CredentialBasedMsiAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters,
            Uri credentialEndpoint)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
            _managedIdentityParameters = managedIdentityParameters;
            _credentialEndpoint = credentialEndpoint;
        }

        public static CredentialBasedMsiAuthRequest TryCreate(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
        {
            if ((serviceBundle.Config.HttpClientFactory != null && 
                serviceBundle.Config.HttpClientFactory is not IMsalMtlsHttpClientFactory) 
                && string.IsNullOrEmpty(managedIdentityParameters.Claims))
            {
                authenticationRequestParameters.RequestContext.Logger.Info($"[Managed Identity] Credential based managed identity is unavailable. {MsalErrorMessage.CredentialHttpCustomizationError}");
                return null;
            }

            return IsCredentialKeyAvailable(authenticationRequestParameters.RequestContext, managedIdentityParameters, out Uri credentialEndpointUri) ?
                    new CredentialBasedMsiAuthRequest(
                    serviceBundle,
                    authenticationRequestParameters,
                    managedIdentityParameters, credentialEndpointUri) :
                    null;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (AuthenticationRequestParameters.Scope == null || AuthenticationRequestParameters.Scope.Count == 0)
            {
                throw new MsalClientException(
                    MsalError.ScopesRequired,
                    MsalErrorMessage.ScopesRequired);
            }

            ILoggerAdapter logger = AuthenticationRequestParameters.RequestContext.Logger;

            AuthenticationResult authResult = null;
            IKeyMaterialManager keyMaterial = base.ServiceBundle.Config.KeyMaterialManagerForTest ??
                AuthenticationRequestParameters.RequestContext.ServiceBundle.PlatformProxy.GetKeyMaterialManager();

            //skip checking cache for force refresh or when claims are present
            if (_managedIdentityParameters.ForceRefresh || !string.IsNullOrEmpty(_managedIdentityParameters.Claims))
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ForceRefreshOrClaims;

                logger.Info("[CredentialBasedMsiAuthRequest] Skipped looking for an Access Token in the cache because " +
                    "ForceRefresh was set.");

                if (ServiceBundle.Config.ManagedIdentityClientCertificate.Thumbprint
                    != keyMaterial.BindingCertificate.Thumbprint)
                {
                    logger.Info("[CredentialBasedMsiAuthRequest] Managed Identity Client Certificate has been renewed. " +
                        "If you had customized httpfactory with certificate then you need to get the new certificate. ");
                }

                authResult = await GetAccessTokenAsync(keyMaterial, cancellationToken, logger).ConfigureAwait(false);
                return authResult;
            }

            //check cache for AT
            MsalAccessTokenCacheItem cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

            if (cachedAccessTokenItem != null)
            {
                //return the token in the cache and check if it needs to be proactively refreshed
                authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);

                try
                {
                    var proactivelyRefresh = SilentRequestHelper.NeedsRefresh(cachedAccessTokenItem);

                    // may fire a request to get a new token in the background when AT needs to be refreshed
                    if (proactivelyRefresh)
                    {
                        AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.ProactivelyRefreshed;

                        SilentRequestHelper.ProcessFetchInBackground(
                        cachedAccessTokenItem,
                        () => GetAccessTokenAsync(keyMaterial, cancellationToken, logger), logger);
                    }
                }
                catch (MsalServiceException e)
                {
                    return await HandleTokenRefreshErrorAsync(e, cachedAccessTokenItem).ConfigureAwait(false);
                }
            }
            else
            {
                //  No AT in the cache 
                if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo != CacheRefreshReason.Expired)
                {
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;
                }

                authResult = await GetAccessTokenAsync(keyMaterial, cancellationToken, logger).ConfigureAwait(false);
            }

            return authResult;
        }

        private async Task<AuthenticationResult> GetAccessTokenAsync(
            IKeyMaterialManager keyMaterial,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            //calls sent to app token provider
            AuthenticationResult authResult = null;
            MsalAccessTokenCacheItem cachedAccessTokenItem = null;

            try
            {
                //allow only one call to the provider 
                logger.Verbose(() => "[CredentialBasedMsiAuthRequest] Entering token acquire for managed identity credential request semaphore.");
                await s_semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                logger.Verbose(() => "[CredentialBasedMsiAuthRequest] Entered token acquire for managed identity credential request semaphore.");

                // Bypass cache and send request to token endpoint, when 
                // 1. Force refresh is requested, or
                // 2. Claims are passed, or 
                // 3. If the AT needs to be refreshed pro-actively 
                if (AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ProactivelyRefreshed ||
                    AuthenticationRequestParameters.RequestContext.ApiEvent.CacheInfo == CacheRefreshReason.ForceRefreshOrClaims)
                {
                    authResult = await GetAccessTokenFromTokenEndpointAsync(keyMaterial, cancellationToken, logger).ConfigureAwait(false);
                }
                else
                {
                    cachedAccessTokenItem = await GetCachedAccessTokenAsync().ConfigureAwait(false);

                    if (cachedAccessTokenItem == null)
                    {
                        authResult = await GetAccessTokenFromTokenEndpointAsync(keyMaterial, cancellationToken, logger).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.Verbose(() => "[CredentialBasedMsiAuthRequest] Getting Access token from cache ...");
                        authResult = CreateAuthenticationResultFromCache(cachedAccessTokenItem);
                    }
                }

                return authResult;
            }
            catch (MsalManagedIdentityException ex)
            {
                logger.Verbose(() => $"[CredentialBasedMsiAuthRequest] Caught an exception. {ex.Message}");
                throw new MsalManagedIdentityException(ex.ErrorCode, ex.Message, ManagedIdentity.ManagedIdentitySource.Credential);
            }
            finally
            {
                s_semaphoreSlim.Release();
                logger.Verbose(() => "[CredentialBasedMsiAuthRequest] Released token acquire for managed identity credential request semaphore.");
            }
        }

        private async Task<AuthenticationResult> GetAccessTokenFromTokenEndpointAsync(
            IKeyMaterialManager keyMaterial,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            try
            {

                logger.Verbose(() => "[CredentialBasedMsiAuthRequest] Getting token from the managed identity endpoint.");

                CredentialResponse credentialResponse =
                    await GetCredentialAssertionAsync(keyMaterial, logger, cancellationToken).ConfigureAwait(false);

                var baseUri = new Uri(credentialResponse.RegionalTokenUrl);
                var tokenUrl = new Uri(baseUri, $"{credentialResponse.TenantId}/oauth2/v2.0/token");

                OAuth2Client client = CreateClientRequest(
                    keyMaterial,
                    AuthenticationRequestParameters.RequestContext.ServiceBundle.HttpManager,
                    credentialResponse);

                MsalTokenResponse msalTokenResponse = await client
                        .GetTokenAsync(tokenUrl,
                        AuthenticationRequestParameters.RequestContext,
                        true,
                        AuthenticationRequestParameters.OnBeforeTokenRequestHandler).ConfigureAwait(false);

                msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

                return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new MsalManagedIdentityException(
                    MsalError.ManagedIdentityUnreachableNetwork, ex.Message, ManagedIdentity.ManagedIdentitySource.Credential);
            }
            catch (MsalServiceException ex)
            {
                logger.Verbose(() => $"[CredentialBasedMsiAuthRequest] Caught an exception. {ex.Message}");
                throw new MsalManagedIdentityException(ex.ErrorCode, ex.Message, ManagedIdentity.ManagedIdentitySource.Credential);
            }
        }

        private async Task<CredentialResponse> GetCredentialAssertionAsync(
            IKeyMaterialManager keyMaterial,
            ILoggerAdapter logger,
            CancellationToken cancellationToken
            )
        {
            var managedIdentityCredentialResponse = new ManagedIdentityCredentialResponse(
                _credentialEndpoint,
                keyMaterial.BindingCertificate,
                AuthenticationRequestParameters.RequestContext,
                cancellationToken);

            CredentialResponse credentialResponse = await managedIdentityCredentialResponse.GetCredentialAsync().ConfigureAwait(false);

            logger.Verbose(() => "[CredentialBasedMsiAuthRequest] A credential was successfully fetched.");

            return credentialResponse;
        }

        private async Task<MsalAccessTokenCacheItem> GetCachedAccessTokenAsync()
        {
            MsalAccessTokenCacheItem cachedAccessTokenItem = await CacheManager.FindAccessTokenAsync().ConfigureAwait(false);

            if (cachedAccessTokenItem != null && !_managedIdentityParameters.ForceRefresh)
            {
                AuthenticationRequestParameters.RequestContext.ApiEvent.IsAccessTokenCacheHit = true;
                Metrics.IncrementTotalAccessTokensFromCache();
                return cachedAccessTokenItem;
            }

            return null;
        }

        // Create an AuthenticationResult based on a cached access token item.
        private AuthenticationResult CreateAuthenticationResultFromCache(MsalAccessTokenCacheItem cachedAccessTokenItem)
        {
            // Create an AuthenticationResult using the provided cachedAccessTokenItem.
            AuthenticationResult authResult = new AuthenticationResult(
                cachedAccessTokenItem,
                null, // No ID token provided for cache-based authentication.
                AuthenticationRequestParameters.AuthenticationScheme,
                AuthenticationRequestParameters.RequestContext.CorrelationId,
                TokenSource.Cache,
                AuthenticationRequestParameters.RequestContext.ApiEvent,
                account: null, // No user account associated with cached token.
                spaAuthCode: null, // No SPA authorization code associated.
                additionalResponseParameters: null // No additional response parameters.
            );

            // Return the created AuthenticationResult.
            return authResult;
        }

        // Override method to return a sorted set of scopes based on the input set.
        protected override SortedSet<string> GetOverriddenScopes(ISet<string> inputScopes)
        {
            // Create a new SortedSet from the inputScopes to ensure a consistent and sorted order.
            return new SortedSet<string>(inputScopes);
        }

        /// <summary>
        /// Creates an OAuth2 client request for fetching the managed identity credential.
        /// </summary>
        /// <param name="keyMaterial"></param>
        /// <param name="httpManager"></param>
        /// <param name="credentialResponse"></param>
        /// <returns></returns>
        private OAuth2Client CreateClientRequest(
            IKeyMaterialManager keyMaterial,
            IHttpManager httpManager,
            CredentialResponse credentialResponse)
        {
            // Initialize an OAuth2 client with logger, HTTP manager, and binding certificate.
            var client = new OAuth2Client(
                AuthenticationRequestParameters.RequestContext.Logger,
                httpManager,
                keyMaterial.BindingCertificate);

            // Convert overridden scopes to a single string.
            string scopes = GetOverriddenScopes(AuthenticationRequestParameters.Scope).AsSingleString();

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

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return null;
        }

        // Check if CredentialKeyType is set to a valid value for Managed Identity.
        private static bool IsCredentialKeyAvailable(
            RequestContext requestContext,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters,
            out Uri credentialEndpointUri)
        {
            credentialEndpointUri = null;

            // If Managed Identity CredentialKeyType is set to None, indicate unavailability.
            if (requestContext.ServiceBundle.Config.ManagedIdentityCredentialKeyType == CryptoKeyType.None)
            {
                requestContext.Logger.Verbose(() => "[Managed Identity] Credential based managed identity is unavailable.");
                return false;
            }

            // Initialize the credentialUri with the constant CredentialEndpoint and API version.
            string credentialUri = Constants.CredentialEndpoint;
            credentialUri += $"?cred-api-version=1.0";

            // Switch based on the type of Managed Identity ID provided.
            switch (requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                // If the ID is of type ClientId, add user assigned client id to the request.
                case ManagedIdentityIdType.ClientId:
                    requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    credentialUri += $"&{Constants.ManagedIdentityClientId}={requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId}";
                    break;

                // If the ID is of type ResourceId, add user assigned resource id to the request.
                case ManagedIdentityIdType.ResourceId:
                    requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                    credentialUri += $"&{Constants.ManagedIdentityResourceId}={requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId}";
                    break;

                // If the ID is of type ObjectId, add user assigned object id to the request.
                case ManagedIdentityIdType.ObjectId:
                    requestContext.Logger.Info("[Managed Identity] Adding user assigned object id to the request.");
                    credentialUri += $"&{Constants.ManagedIdentityObjectId}={requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId}";
                    break;
            }

            // Set the credentialEndpointUri with the constructed URI.
            credentialEndpointUri = new Uri(credentialUri);

            // Log information about creating Credential based managed identity.
            requestContext.Logger.Info($"[Managed Identity] Creating Credential based managed identity.");
            return true;
        }
    }
}
