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

namespace Microsoft.Identity.Client.Credential
{
    internal class ManagedIdentityCredentialResponseCache : IManagedIdentityCredentialResponseCache
    {
        private readonly ConcurrentDictionary<string, CredentialResponse> _cache = new();
        private readonly Uri _uri;
        private readonly ManagedIdentityId _managedIdentityId;
        private readonly X509Certificate2 _bindingCertificate;
        private readonly AcquireTokenForManagedIdentityParameters _managedIdentityParameters;
        private readonly RequestContext _requestContext;
        private readonly CancellationToken _cancellationToken;

        public ManagedIdentityCredentialResponseCache(
            Uri uri,
            ManagedIdentityId managedIdentityId,
            X509Certificate2 bindingCertificate,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters,
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            _uri = uri;
            _managedIdentityId = managedIdentityId;
            _bindingCertificate = bindingCertificate;
            _managedIdentityParameters = managedIdentityParameters;
            _requestContext = requestContext;
            _cancellationToken = cancellationToken;
        }

        public async Task<CredentialResponse> GetOrFetchCredentialAsync() 
        {
            string cacheKey = _managedIdentityId.ToString();

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

                if (expiresOnDateTime > DateTimeOffset.UtcNow && !_managedIdentityParameters.ForceRefresh)
                {
                    // Cache hit and not expired
                    _requestContext.Logger.Info("[Managed Identity] Returned cached credential response.");
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
                _requestContext.Logger.Error("[Managed Identity] Credential Response is null or insufficient for authentication.");
                throw new MsalManagedIdentityException(
                    MsalError.ManagedIdentityRequestFailed,
                    MsalErrorMessage.ManagedIdentityInvalidResponse,
                    ManagedIdentitySource.Credential);
            }

            AddCredential(cacheKey, credentialResponse);
            return credentialResponse;
            
        }

        public void AddCredential(string key, CredentialResponse response)
        {
            _cache[key] = response;
        }

        public void RemoveCredential(string key)
        {
            _cache.TryRemove(key, out _);
        }

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
                // Handle the exception (log, rethrow, etc.)
                _requestContext.Logger.Error("[Managed Identity] Error fetching credential from IMDS endpoint: " + ex.Message);
                throw; // Rethrow the exception or handle as appropriate for your scenario
            }
        }

        private OAuth2Client CreateClientRequest(IHttpManager httpManager)
        {
            var client = new OAuth2Client(_requestContext.Logger, httpManager, null);

            client.AddHeader("Metadata", "true");
            client.AddHeader("x-ms-client-request-id", _requestContext.CorrelationId.ToString("D"));
            // client.AddQueryParameter("cred-api-version", "1.0");

            switch (_requestContext.ServiceBundle.Config.ManagedIdentityId.IdType)
            {
                case AppConfig.ManagedIdentityIdType.ClientId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned client id to the request.");
                    client.AddQueryParameter(Constants.ManagedIdentityClientId, _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);
                    break;

                case AppConfig.ManagedIdentityIdType.ResourceId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned resource id to the request.");
                    client.AddQueryParameter(Constants.ManagedIdentityResourceId, _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);
                    break;

                case AppConfig.ManagedIdentityIdType.ObjectId:
                    _requestContext.Logger.Info("[Managed Identity] Adding user assigned object id to the request.");
                    client.AddQueryParameter(Constants.ManagedIdentityObjectId, _requestContext.ServiceBundle.Config.ManagedIdentityId.UserAssignedId);
                    break;
            }

            string jsonPayload = CreateCredentialPayload(_bindingCertificate);
            client.AddBodyContent(new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json"));

            return client;
        }

        private static string CreateCredentialPayload(X509Certificate2 x509Certificate2)
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
