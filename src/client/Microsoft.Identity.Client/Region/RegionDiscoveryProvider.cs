// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Region
{
    internal sealed class RegionDiscoveryProvider : IRegionDiscoveryProvider
    {
        private const string RegionName = "REGION_NAME";

        // For information of the current api-version refer: https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#versioning
        private const string ImdsEndpoint = "http://169.254.169.254/metadata/instance/compute/location";
        private const string DefaultApiVersion = "2020-06-01";

        private readonly IHttpManager _httpManager;
        private readonly INetworkCacheMetadataProvider _networkCacheMetadataProvider;
        private readonly int _imdsCallTimeoutMs;
        private static string s_region;

        public RegionDiscoveryProvider(
            IHttpManager httpManager, 
            INetworkCacheMetadataProvider networkCacheMetadataProvider = null, 
            int imdsCallTimeout = 2000,
            /* for tests only */ bool shouldClearCaches = false)
        {
            _httpManager = httpManager;
            _networkCacheMetadataProvider = networkCacheMetadataProvider ?? new NetworkCacheMetadataProvider();
            _imdsCallTimeoutMs = imdsCallTimeout;

            if (shouldClearCaches)
            {
                Clear();
            }
        }

        /*
         * To be used only for tests
         */
        public void Clear()
        {
            s_region = null;
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            Uri regionalizedAuthority = await BuildAuthorityWithRegionAsync(authority, requestContext).ConfigureAwait(false);
            InstanceDiscoveryMetadataEntry cachedEntry = _networkCacheMetadataProvider.GetMetadata(regionalizedAuthority.Host, requestContext.Logger);

            if (cachedEntry == null)
            {
                CacheInstanceDiscoveryMetadata(CreateEntry(authority, regionalizedAuthority));

                cachedEntry = _networkCacheMetadataProvider.GetMetadata(regionalizedAuthority.Host, requestContext.Logger);
                requestContext.Logger.Verbose($"[Region Discovery] Created metadata for the regional environment {regionalizedAuthority.Host} ? {cachedEntry != null}");
            }
            else
            {
                requestContext.Logger.Verbose($"[Region Discovery] The network provider found an entry for {regionalizedAuthority.Host}");
                LogTelemetryData(cachedEntry.PreferredNetwork.Split('.')[0], RegionSource.Cache, requestContext);
            }
            
            return cachedEntry;
        }

        private async Task<string> GetRegionAsync(RequestContext requestContext)
        {
            string region = Environment.GetEnvironmentVariable(RegionName);

            if (!string.IsNullOrEmpty(region))
            {
                requestContext.Logger.Info($"[Region discovery] Region found in environment variable: {region}.");

                LogTelemetryData(region, RegionSource.EnvVariable, requestContext);

                return region;
            }

            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Metadata", "true" }
                };

                HttpResponse response = await _httpManager.SendGetAsync(BuildImdsUri(DefaultApiVersion), headers, requestContext.Logger, retry: false, GetCancellationToken(requestContext.UserCancellationToken)).ConfigureAwait(false);

                // A bad request occurs when the version in the IMDS call is no longer supported.
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string apiVersion = await GetImdsUriApiVersionAsync(requestContext.Logger, headers, requestContext.UserCancellationToken).ConfigureAwait(false); // Get the latest version
                    response = await _httpManager.SendGetAsync(BuildImdsUri(apiVersion), headers, requestContext.Logger, retry: false, GetCancellationToken(requestContext.UserCancellationToken)).ConfigureAwait(false); // Call again with updated version
                }

                if (response.StatusCode == HttpStatusCode.OK && !response.Body.IsNullOrEmpty())
                {
                    region = response.Body;
                    LogTelemetryData(region, RegionSource.Imds, requestContext);
                    return region;
                }

                requestContext.Logger.Info($"[Region discovery] Call to local IMDS failed with status code: {response.StatusCode} or an empty response.");

                throw MsalServiceExceptionFactory.FromImdsResponse(
                    MsalError.RegionDiscoveryFailed,
                    MsalErrorMessage.RegionDiscoveryFailed,
                    response);
            }  
            catch (MsalServiceException e)
            {
                if (MsalError.RequestTimeout.Equals(e.ErrorCode))
                {
                    throw new MsalServiceException(MsalError.RegionDiscoveryFailed, MsalErrorMessage.RegionDiscoveryFailedWithTimeout, e);
                }

                throw e;
            }
            catch (Exception e)
            {
                requestContext.Logger.Info("[Region discovery] Call to local imds failed. " + e);
                throw new MsalServiceException(MsalError.RegionDiscoveryFailed, MsalErrorMessage.RegionDiscoveryFailed, e);
            }
        }

        private CancellationToken GetCancellationToken(CancellationToken userCancellationToken)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource(_imdsCallTimeoutMs);
            CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(userCancellationToken, tokenSource.Token);

            return linkedTokenSource.Token;
        }

        private void LogTelemetryData(string region, RegionSource regionSource, RequestContext requestContext)
        {
            requestContext.ApiEvent.RegionDiscovered = region;
            requestContext.ApiEvent.RegionSource = (int) regionSource;
            requestContext.ApiEvent.UserProvidedRegion = requestContext.ServiceBundle.Config.AuthorityInfo.RegionToUse;
        }

        private async Task<string> GetImdsUriApiVersionAsync(ICoreLogger logger, Dictionary<string, string> headers, CancellationToken userCancellationToken)
        {
            Uri imdsErrorUri = new Uri(ImdsEndpoint);

            HttpResponse response = await _httpManager.SendGetAsync(imdsErrorUri, headers, logger, retry: false, GetCancellationToken(userCancellationToken)).ConfigureAwait(false);

            // When IMDS endpoint is called without the api version query param, bad request response comes back with latest version.
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                LocalImdsErrorResponse errorResponse = JsonHelper.DeserializeFromJson<LocalImdsErrorResponse>(response.Body);

                if (errorResponse != null && !errorResponse.NewestVersions.IsNullOrEmpty())
                {
                    logger.Info("[Region discovery] Updated the version for IMDS endpoint to: " + errorResponse.NewestVersions[0]);
                    return errorResponse.NewestVersions[0];
                }

                logger.Info("[Region Discovery] The response is empty or does not contain the newest versions.");
            }

            logger.Info($"[Region Discovery] Failed to get the updated version for IMDS endpoint. HttpStatusCode: {response.StatusCode}");

            throw MsalServiceExceptionFactory.FromImdsResponse(
            MsalError.RegionDiscoveryFailed,
            MsalErrorMessage.RegionDiscoveryFailed,
            response);
        }

        private Uri BuildImdsUri(string apiVersion)
        {
            UriBuilder uriBuilder = new UriBuilder(ImdsEndpoint);
            uriBuilder.AppendQueryParameters($"api-version={apiVersion}");
            uriBuilder.AppendQueryParameters("format=text");
            return uriBuilder.Uri;
        }

        private static InstanceDiscoveryMetadataEntry CreateEntry(Uri orginalAuthority, Uri regionalizedAuthority)
        {
            return new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { regionalizedAuthority.Host },
                PreferredCache = orginalAuthority.Host,
                PreferredNetwork = regionalizedAuthority.Host
            };
        }

        private void CacheInstanceDiscoveryMetadata(InstanceDiscoveryMetadataEntry metadataEntry)
        {
            foreach (string aliasedEnvironment in metadataEntry.Aliases ?? Enumerable.Empty<string>())
            {
                _networkCacheMetadataProvider.AddMetadata(aliasedEnvironment, metadataEntry);
            }
        }

        private async Task<Uri> BuildAuthorityWithRegionAsync(Uri canonicalAuthority, RequestContext requestContext)
        {
            string regionToUse = requestContext.ServiceBundle.Config.AuthorityInfo.RegionToUse;

            if (s_region.IsNullOrEmpty())
            {
                try
                {
                    s_region = await GetRegionAsync(requestContext).ConfigureAwait(false);

                    if (!regionToUse.IsNullOrEmpty())
                    {
                        requestContext.ApiEvent.IsValidUserProvidedRegion = s_region.Equals(regionToUse);
                        requestContext.Logger.Info($"The auto detected region is {s_region}.");
                        if (s_region.Equals(regionToUse))
                        {
                            requestContext.Logger.Info("The region provided by the user is valid and equal to the auto detected region.");
                        }
                        else
                        {
                            requestContext.Logger.Info($"The region provided by the user is invalid. Region detected: {s_region} Region provided: {regionToUse}");
                        }
                    }
                }
                catch (MsalServiceException e)
                {
                    if (regionToUse.IsNullOrEmpty())
                    {
                        throw e;
                    }

                    s_region = regionToUse;
                    requestContext.Logger.Info($"Region auto detection failed. Region provided by the user will be used: ${regionToUse}.");
                    LogTelemetryData(s_region, RegionSource.UserProvided, requestContext);
                }
            }

            var builder = new UriBuilder(canonicalAuthority);

            if (KnownMetadataProvider.IsPublicEnvironment(canonicalAuthority.Host))
            {
                builder.Host = $"{s_region}.login.microsoft.com";
            }
            else
            {
                builder.Host = $"{s_region}.{builder.Host}";
            }

            return builder.Uri;
        }
    }
}
