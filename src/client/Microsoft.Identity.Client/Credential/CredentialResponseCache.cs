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
using System.Text.Json;
using Microsoft.Identity.Client.Credential;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class CredentialResponseCache : ICredentialResponseCache
    {
        private readonly ConcurrentDictionary<string, HttpResponse> _cache = new();
        private static readonly object s_lock = new();
        private readonly RequestContext _requestContext;
        private readonly SemaphoreSlim _cacheLock = new (1, 1); // Initialize with 1 concurrency level

        // Create a private static instance of the cache
        private static CredentialResponseCache s_instance;

        // Create a private constructor to prevent external instantiation
        private CredentialResponseCache(RequestContext requestContext)
        {
            _requestContext = requestContext;
        }

        // Create a public method to get the instance of the cache
        public static CredentialResponseCache GetCredentialInstance(RequestContext requestContext)
        {
            lock (s_lock)
            {
                return s_instance ??= new CredentialResponseCache(requestContext);

            }
        }

        public async Task<HttpResponse> GetOrFetchCredentialAsync(
            ManagedIdentityRequest request, 
            string key,
            CancellationToken cancellationToken)
        {
            await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_cache.TryGetValue(key, out HttpResponse response))
                {
                    long expiresOnSeconds = GetExpiresOnFromJson(response.Body);
                    DateTimeOffset expiresOnDateTime = DateTimeOffset.FromUnixTimeSeconds(expiresOnSeconds);

                    if (expiresOnDateTime > DateTimeOffset.UtcNow)
                    {
                        // Cache hit and not expired
                        _requestContext.Logger.Info("[Managed Identity] Returned cached credential response.");
                        return response;
                    }
                    else
                    {
                        // Cache hit but expired, remove it
                        _requestContext.Logger.Info("[Managed Identity] Cached credential expired.");
                        RemoveCredential(key);
                    }
                }

                HttpResponse httpResponse = await FetchFromServiceAsync(request, cancellationToken).ConfigureAwait(false);
                AddCredential(key, httpResponse);
                return httpResponse;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public void AddCredential(string key, HttpResponse response)
        {
            _cache[key] = response;
        }

        public void RemoveCredential(string key)
        {
            _cache.TryRemove(key, out _);
        }

        private async Task<HttpResponse> FetchFromServiceAsync(
            ManagedIdentityRequest request,
            CancellationToken cancellationToken)
        {
            _requestContext.Logger.Info("[Managed Identity] Fetching credential from IMDS endpoint.");

            return await _requestContext.ServiceBundle.HttpManager
                            .SendPostForceResponseAsync(
                                request.ComputeUri(),
                                request.Headers,
                                request.Content,
                                _requestContext.Logger, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
        }

        private long GetExpiresOnFromJson(string jsonContent)
        {
            // Parse the JSON content to extract ExpiresOn
            CredentialResponse credentialResponse = JsonHelper.DeserializeFromJson<CredentialResponse>(jsonContent);

            if (credentialResponse != null)
            {
                return credentialResponse.ExpiresOn;
            }

            return 0; 
        }
    }
}
