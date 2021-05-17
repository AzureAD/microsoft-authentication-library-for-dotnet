// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Region
{
    internal sealed class RegionManager : IRegionManager
    {
        private class RegionInfo
        {
            public RegionInfo(string region, RegionSource regionSource)
            {
                Region = region;
                RegionSource = regionSource;
            }

            public string Region { get; }
            public RegionSource RegionSource { get; }
        }

        // For information of the current api-version refer: https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#versioning
        private const string ImdsEndpoint = "http://169.254.169.254/metadata/instance/compute/location";
        private const string DefaultApiVersion = "2020-06-01";

        private readonly IHttpManager _httpManager;
        private readonly int _imdsCallTimeoutMs;

        private static string s_autoDiscoveredRegion;
        private static bool s_failedAutoDiscovery = false;

        public RegionManager(
            IHttpManager httpManager,
            int imdsCallTimeout = 2000,
            bool shouldClearStaticCache = false) // for test
        {
            _httpManager = httpManager;
            _imdsCallTimeoutMs = imdsCallTimeout;

            if (shouldClearStaticCache)
            {
                s_failedAutoDiscovery = false;
                s_autoDiscoveredRegion = null;
            }
        }

        public async Task<string> GetAzureRegionAsync(RequestContext requestContext)
        {
            string azureRegionConfig = requestContext.ServiceBundle.Config.AzureRegion;
            var logger = requestContext.Logger;
            if (string.IsNullOrEmpty(azureRegionConfig))
            {
                logger.Verbose($"[Region discovery] WithAzureRegion not configured. ");
                return null;
            }

            // MSAL always performs region auto-discovery, even if the user configured an actual region
            // in order to detect inconsistencies and report via telemetry
            var discoveredRegion = await DiscoverAndCacheAsync(azureRegionConfig, logger, requestContext.UserCancellationToken).ConfigureAwait(false);

            RecordTelemetry(requestContext.ApiEvent, azureRegionConfig, discoveredRegion);

            if (IsAutoDiscoveryRequested(azureRegionConfig))
            {
                if (discoveredRegion.RegionSource != RegionSource.FailedAutoDiscovery)
                {
                    return discoveredRegion.Region;
                }
                else
                {
                    logger.Warning($"[Region discovery] Auto-discovery failed.");
                    return null;
                }
            }

            logger.Info($"[Region discovery] Returning user provided region: {azureRegionConfig}.");
            return azureRegionConfig;
        }

        private static bool IsAutoDiscoveryRequested(string azureRegionConfig)
        {
            return string.Equals(azureRegionConfig, ConfidentialClientApplication.AttemptRegionDiscovery);
        }

        private void RecordTelemetry(ApiEvent apiEvent, string azureRegionConfig, RegionInfo discoveredRegion)
        {
            // already emitted telemetry for this request, don't emit again as it will overwrite with "from cache"
            if (IsTelemetryRecorded(apiEvent))
            {
                return;
            }

            bool isAutoDiscoveryRequested = IsAutoDiscoveryRequested(azureRegionConfig);

            if (isAutoDiscoveryRequested)
            {
                apiEvent.RegionUsed = discoveredRegion.Region;
                if (discoveredRegion.RegionSource != RegionSource.FailedAutoDiscovery)
                {
                    apiEvent.RegionSource = (int)discoveredRegion.RegionSource;
                }
                apiEvent.FallbackToGlobal = discoveredRegion.Region == null;
                apiEvent.IsValidUserProvidedRegion = null;
            }
            else
            {
                apiEvent.RegionUsed = azureRegionConfig;

                if (discoveredRegion.RegionSource == RegionSource.FailedAutoDiscovery)
                {
                    apiEvent.RegionSource = (int)RegionSource.UserProvided;
                }
                else
                {
                    apiEvent.RegionSource = (int)discoveredRegion.RegionSource;
                }

                apiEvent.UserProvidedRegion = azureRegionConfig;
                apiEvent.FallbackToGlobal = false;
                if (!string.IsNullOrEmpty(discoveredRegion.Region))
                {
                    apiEvent.IsValidUserProvidedRegion = string.Equals(
                        discoveredRegion.Region,
                        azureRegionConfig);
                }
            }
        }

        private bool IsTelemetryRecorded(ApiEvent apiEvent)
        {
            return 
                !(apiEvent.RegionUsed == null &&
                 apiEvent.RegionSource == (int)(default(RegionSource)) &&
                 apiEvent.UserProvidedRegion == null &&
                 apiEvent.FallbackToGlobal == null &&
                 apiEvent.IsValidUserProvidedRegion == null);
        }

        private async Task<RegionInfo> DiscoverAndCacheAsync(string azureRegionConfig, ICoreLogger logger, CancellationToken requestCancellationToken)
        {
            if (s_failedAutoDiscovery == true)
            {
                logger.Info($"[Region discovery] Auto-discovery failed in the past. Not trying again.");
                return new RegionInfo(null, RegionSource.FailedAutoDiscovery);
            }

            if (s_failedAutoDiscovery == false &&
                !string.IsNullOrEmpty(s_autoDiscoveredRegion))
            {
                logger.Info($"[Region discovery] Auto-discovery already ran and found {s_autoDiscoveredRegion}.");
                return new RegionInfo(s_autoDiscoveredRegion, RegionSource.Cache);
            }

            var result = await DiscoverAsync(logger, requestCancellationToken).ConfigureAwait(false);

            s_failedAutoDiscovery = result.RegionSource == RegionSource.FailedAutoDiscovery;
            s_autoDiscoveredRegion = result.Region;

            return result;
        }

        private async Task<RegionInfo> DiscoverAsync(ICoreLogger logger, CancellationToken requestCancellationToken)
        {
            string region = Environment.GetEnvironmentVariable("REGION_NAME");

            if (!string.IsNullOrEmpty(region))
            {
                logger.Info($"[Region discovery] Region found in environment variable: {region}.");
                return new RegionInfo(region, RegionSource.EnvVariable);
            }

            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Metadata", "true" }
                };

                HttpResponse response = await _httpManager.SendGetAsync(BuildImdsUri(DefaultApiVersion), headers, logger, retry: false, GetCancellationToken(requestCancellationToken))
                    .ConfigureAwait(false);

                // A bad request occurs when the version in the IMDS call is no longer supported.
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    string apiVersion = await GetImdsUriApiVersionAsync(logger, headers, requestCancellationToken).ConfigureAwait(false); // Get the latest version
                    response = await _httpManager.SendGetAsync(BuildImdsUri(apiVersion), headers, logger, retry: false, GetCancellationToken(requestCancellationToken))
                        .ConfigureAwait(false); // Call again with updated version
                }

                if (response.StatusCode == HttpStatusCode.OK && !response.Body.IsNullOrEmpty())
                {
                    region = response.Body;

                    logger.Info($"[Region discovery] Call to local IMDS succeeded. Region: {region}.");
                    return new RegionInfo(region, RegionSource.Imds);
                }

                logger.Warning($"[Region discovery] Call to local IMDS failed with status code {response.StatusCode} or an empty response.");

            }
            catch (Exception e)
            {
                if (e is MsalServiceException msalEx && MsalError.RequestTimeout.Equals(msalEx?.ErrorCode))
                {
                    logger.Warning($"[Region discovery] Call to local IMDS timed out after {_imdsCallTimeoutMs}.");
                }
                else
                {
                    logger.Warning($"[Region discovery] IMDS call failed with exception {e}");
                }
            }

            return new RegionInfo(null, RegionSource.FailedAutoDiscovery);
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
                    logger.Info($"[Region discovery] Updated the version for IMDS endpoint to: {errorResponse.NewestVersions[0]}.");
                    return errorResponse.NewestVersions[0];
                }

                logger.Info("[Region discovery] The response is empty or does not contain the newest versions.");
            }

            logger.Info($"[Region discovery] Failed to get the updated version for IMDS endpoint. HttpStatusCode: {response.StatusCode}.");

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

        private CancellationToken GetCancellationToken(CancellationToken userCancellationToken)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource(_imdsCallTimeoutMs);
            CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(userCancellationToken, tokenSource.Token);

            return linkedTokenSource.Token;
        }
    }
}
