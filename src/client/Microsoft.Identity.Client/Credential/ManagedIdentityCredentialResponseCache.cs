// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Core;
using System.Collections.Concurrent;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using System.Web;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal.Requests;

namespace Microsoft.Identity.Client.Credential
{
    /// <summary>
    /// Represents a cache for managing and storing Managed Identity credentials.
    /// </summary>
    internal class ManagedIdentityCredentialResponseCache : IManagedIdentityCredentialResponseCache
    {
        private readonly ConcurrentDictionary<string, CredentialResponse> _cache = new();
        private readonly Uri _uri;
        private readonly string _clientId;
        private readonly X509Certificate2 _bindingCertificate;
        private readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        private readonly RequestContext _requestContext;
        private readonly CancellationToken _cancellationToken;

        public ManagedIdentityCredentialResponseCache(
            Uri uri,
            string clientId,
            X509Certificate2 bindingCertificate,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            _uri = uri;
            _clientId = clientId;
            _bindingCertificate = bindingCertificate;
            _managedIdentityParameters = managedIdentityParameters;
            _requestContext = requestContext;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Gets or fetches the Managed Identity credential from the cache or the service.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MsalManagedIdentityException"></exception>
        public async Task<CredentialResponse> GetOrFetchCredentialAsync() 
        {
            string cacheKey = _clientId;

            if (_cache.TryGetValue(cacheKey, out CredentialResponse response))
            {
                long expiresOnSeconds = response.ExpiresOn;
#if NET45
                DateTimeOffset expiresOnDateTime = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)
                                                            .AddSeconds(expiresOnSeconds)
                                                            .ToLocalTime();
#else
                DateTimeOffset expiresOnDateTime = DateTimeOffset.FromUnixTimeSeconds(expiresOnSeconds);
#endif
                //Credential expires in 15 minutes, having a 60 second buffer before we request a new credential
                const int expirationBufferSeconds = 60; 

                if (expiresOnDateTime > DateTimeOffset.UtcNow.AddSeconds(-expirationBufferSeconds) || 
                    !_managedIdentityParameters.ForceRefresh ||
                    !string.IsNullOrEmpty(_managedIdentityParameters.Claims)
                    )
                {
                    // Cache hit and not expired
                    _requestContext.Logger.Info("[Managed Identity] Returned cached credential response.");
                    _requestContext.ApiEvent.CredentialSource = TokenSource.Cache;
                    return response;
                }
                else
                {
                    // Cache hit but expired or force refresh was set, remove cached credential
                    _requestContext.Logger.Info("[Managed Identity] Cached credential expired or force refresh was set.");
                    RemoveCredential(cacheKey);
                }
            }

            CredentialResponse credentialResponse = await FetchFromServiceAsync(
                _requestContext.ServiceBundle.HttpManager, 
                _cancellationToken
                ).ConfigureAwait(false);

            if (credentialResponse == null || credentialResponse.Credential.IsNullOrEmpty())
            {
                _requestContext.Logger.Error("[Managed Identity] Credential Response is null " +
                    "or insufficient for authentication.");
                
                throw new MsalManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidResponse,
                    ManagedIdentitySource.Credential);
            }

            AddCredential(cacheKey, credentialResponse);
            _requestContext.ApiEvent.CredentialSource = TokenSource.IdentityProvider;
            return credentialResponse;
        }

        /// <summary>
        /// Adds a credential response to the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="response"></param>
        public void AddCredential(string key, CredentialResponse response)
        {
            _cache[key] = response;
        }

        /// <summary>
        /// Removes a credential from the cache.
        /// </summary>
        /// <param name="key"></param>
        public void RemoveCredential(string key)
        {
            _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// Fetches a new managed identity credential from the IMDS endpoint.
        /// </summary>
        /// <param name="httpManager"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="MsalServiceException"></exception>
        private async Task<CredentialResponse> FetchFromServiceAsync(
            IHttpManager httpManager,
            CancellationToken cancellationToken)
        {
            try
            {
                _requestContext.Logger.Info("[Managed Identity] Fetching new managed identity credential from IMDS endpoint.");

                OAuth2Client client = CreateClientRequest(httpManager);

                CredentialResponse credentialResponse = await client
                    .GetCredentialResponseAsync(_uri, _requestContext)
                    .ConfigureAwait(false);

                return credentialResponse;

            }
            catch (Exception ex)
            {
                _requestContext.Logger.Error("[Managed Identity] Error fetching credential from IMDS endpoint: " + ex.Message);

                throw new MsalManagedIdentityException(
                    MsalError.CredentialRequestFailed, 
                    MsalErrorMessage.CredentialEndpointNoResponseReceived, 
                    ManagedIdentitySource.Credential);
                ; 
            }
        }

        /// <summary>
        /// Creates an OAuth2 client request for fetching the managed identity credential.
        /// </summary>
        /// <param name="httpManager"></param>
        /// <returns></returns>
        private OAuth2Client CreateClientRequest(IHttpManager httpManager)
        {
            var client = new OAuth2Client(_requestContext.Logger, httpManager, null);

            client.AddHeader("Metadata", "true");
            client.AddHeader("x-ms-client-request-id", _requestContext.CorrelationId.ToString("D"));
            // client.AddQueryParameter("cred-api-version", "1.0");

            switch (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case ManagedIdentityIdType.ClientId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    client.AddQueryParameter(Constants.ManagedIdentityClientId, _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);
                    break;

                case ManagedIdentityIdType.ResourceId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                    client.AddQueryParameter(Constants.ManagedIdentityResourceId, _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);
                    break;

                case ManagedIdentityIdType.ObjectId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned object id to the request.");
                    client.AddQueryParameter(Constants.ManagedIdentityObjectId, _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);
                    break;
            }

            string jsonPayload = GetCredentialPayload(_bindingCertificate);
            client.AddBodyContent(new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json"));

            return client;
        }

        /// <summary>
        /// Creates the payload for the managed identity credential request.
        /// </summary>
        /// <param name="x509Certificate2"></param>
        /// <returns></returns>
        private static string GetCredentialPayload(X509Certificate2 x509Certificate2)
        {
            string certificateBase64 = Convert.ToBase64String(x509Certificate2.Export(X509ContentType.Cert));

            return @"
                    {
                        ""cnf"": {
                            ""jwk"": {
                                ""kty"": ""RSA"", 
                                ""use"": ""sig"",
                                ""alg"": ""RS256"",
                                ""kid"": """ + x509Certificate2.Thumbprint + @""",
                                ""x5c"": [""" + certificateBase64 + @"""]
                            }
                        },
                        ""latch_key"": false    
                    }";
        }
    }
}
