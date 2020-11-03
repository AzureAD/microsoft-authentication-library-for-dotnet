// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Region
{
    internal sealed class RegionDiscoveryProvider : IRegionDiscoveryProvider
    {
        private const string RegionName = "REGION_NAME";

        // For information of the current api-version refer: https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#versioning
        private const string ImdsEndpoint = "http://169.254.169.254/metadata/instance/compute";
        private const string DefaultApiVersion = "2020-06-01";

        private readonly IHttpManager _httpManager;
        private readonly INetworkCacheMetadataProvider _networkCacheMetadataProvider;

        public RegionDiscoveryProvider(IHttpManager httpManager, INetworkCacheMetadataProvider networkCacheMetadataProvider = null)
        {
            _httpManager = httpManager;
            _networkCacheMetadataProvider = networkCacheMetadataProvider ?? new NetworkCacheMetadataProvider();
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            ICoreLogger logger = requestContext.Logger;
            string environment = authority.Host;
            InstanceDiscoveryMetadataEntry cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);

            if (cachedEntry == null)
            {
                Uri regionalizedAuthority = await BuildAuthorityWithRegionAsync(authority, requestContext.Logger).ConfigureAwait(false);
                CacheInstanceDiscoveryMetadata(CreateEntry(authority, regionalizedAuthority));

                cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);
                logger.Verbose($"[Region Discovery] Created metadata for the regional environment {environment} ? {cachedEntry != null}");
            }
            else
            {
                logger.Verbose($"[Region Discovery] The network provider found an entry for {environment}");
            }

            requestContext.ApiEvent.RegionDiscovered = cachedEntry.PreferredNetwork.Split('.')[0];
            return cachedEntry;
        }


        private async Task<string> GetRegionAsync(ICoreLogger logger)
        {
            if (!Environment.GetEnvironmentVariable(RegionName).IsNullOrEmpty())
            {
                logger.Info($"[Region discovery] Region: {Environment.GetEnvironmentVariable(RegionName)}");
                return Environment.GetEnvironmentVariable(RegionName);
            }

            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Metadata", "true" }
                };
                
                HttpResponse response = await _httpManager.SendGetAsync(BuildImdsUri(DefaultApiVersion), headers, logger).ConfigureAwait(false);

                // A bad request occurs when the version in the IMDS call is no longer supported.
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string apiVersion = await GetImdsUriApiVersionAsync(logger, headers).ConfigureAwait(false); // Get the latest version
                    response = await _httpManager.SendGetAsync(BuildImdsUri(apiVersion), headers, logger).ConfigureAwait(false); // Call again with updated version
                }

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw MsalServiceExceptionFactory.FromGeneralHttpResponse(
                    MsalError.RegionDiscoveryFailed,
                    MsalErrorMessage.RegionDiscoveryFailed,
                    response);
                }

                LocalImdsResponse localImdsResponse = JsonHelper.DeserializeFromJson<LocalImdsResponse>(response.Body);

                logger.Info($"[Region discovery] Call to local IMDS returned region: {localImdsResponse.location}");
                return localImdsResponse.location;
            }
            catch (MsalServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.Info("[Region discovery] Call to local imds failed." + e.Message);
                throw new MsalServiceException(MsalError.RegionDiscoveryFailed, MsalErrorMessage.RegionDiscoveryFailed);
            }
        }

        private async Task<string> GetImdsUriApiVersionAsync(ICoreLogger logger, Dictionary<string, string> headers)
        {
            Uri imdsErrorUri = new Uri(ImdsEndpoint);

            HttpResponse response = await _httpManager.SendGetAsync(imdsErrorUri, headers, logger).ConfigureAwait(false);

            // When IMDS endpoint is called without the api version query param, bad request response comes back with latest version.
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                LocalImdsErrorResponse errorResponse = JsonHelper.DeserializeFromJson<LocalImdsErrorResponse>(response.Body);
                logger.Info("[Region discovery] Updated the version for IMDS endpoint to: " + errorResponse.NewestVersions[0]);
                return errorResponse.NewestVersions[0];
            }

            logger.Info("[Region Discovery] Failed to get the updated version for IMDS endpoint.");

            throw MsalServiceExceptionFactory.FromGeneralHttpResponse(
            MsalError.RegionDiscoveryFailed,
            MsalErrorMessage.RegionDiscoveryFailed,
            response);
        }

        private Uri BuildImdsUri(string apiVersion)
        {
            UriBuilder uriBuilder = new UriBuilder(ImdsEndpoint);
            uriBuilder.AppendQueryParameters($"api-version={apiVersion}");
            return uriBuilder.Uri;
        }

        private static InstanceDiscoveryMetadataEntry CreateEntry(Uri orginalAuthority, Uri regionalizedAuthority)
        {
            return new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { orginalAuthority.Host, regionalizedAuthority.Host },
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

        private async Task<Uri> BuildAuthorityWithRegionAsync(Uri canonicalAuthority, ICoreLogger logger)
        {
            string region = await GetRegionAsync(logger).ConfigureAwait(false);
            var builder = new UriBuilder(canonicalAuthority);

            if (KnownMetadataProvider.IsPublicEnvironment(canonicalAuthority.Host))
            {
                builder.Host = $"{region}.login.microsoft.com";
            }
            else
            {
                builder.Host = $"{region}.{builder.Host}";
            }

            return builder.Uri;
        }
    }
}
