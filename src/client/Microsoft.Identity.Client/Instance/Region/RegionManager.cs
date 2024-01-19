// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            public RegionInfo(string region, RegionAutodetectionSource regionSource, string regionDetails)
            {
                Region = region;
                RegionSource = regionSource;
                RegionDetails = regionDetails;
            }

            public string Region { get; }
            public RegionAutodetectionSource RegionSource { get; }
            public readonly string RegionDetails;
        }

        // For information of the current api-version refer: https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#versioning
        private const string ImdsEndpoint = "http://169.254.169.254/metadata/instance/compute/location";
        private const string DefaultApiVersion = "2020-06-01";

        private readonly IHttpManager _httpManager;
        private readonly int _imdsCallTimeoutMs;

        // lock for discovery. So it is done only once
        private static readonly SemaphoreSlim _lockDiscover = new SemaphoreSlim(1);

        private static string s_autoDiscoveredRegion;
        private static bool s_failedAutoDiscovery = false;
        private static string s_regionDiscoveryDetails;

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
                s_regionDiscoveryDetails = null;
            }
        }

        public async Task<string> GetAzureRegionAsync(RequestContext requestContext)
        {
            string azureRegionConfig = requestContext.ServiceBundle.Config.AzureRegion;
            var logger = requestContext.Logger;
            if (string.IsNullOrEmpty(azureRegionConfig))
            {
                logger.Verbose(() => $"[Region discovery] WithAzureRegion not configured. ");
                return null;
            }

            Debug.Assert(
                requestContext.ApiEvent != null,
                "Do not call GetAzureRegionAsync outside of a request. This can happen if you perform instance discovery outside a request, for example as part of validating input params.");

            // MSAL always performs region auto-discovery, even if the user configured an actual region
            // in order to detect inconsistencies and report via telemetry
            var discoveredRegion = await DiscoverAndCacheAsync(logger, requestContext.UserCancellationToken).ConfigureAwait(false);

            RecordTelemetry(requestContext.ApiEvent, azureRegionConfig, discoveredRegion);

            if (IsAutoDiscoveryRequested(azureRegionConfig))
            {
                if (discoveredRegion.RegionSource != RegionAutodetectionSource.FailedAutoDiscovery)
                {
                    logger.Verbose(() => $"[Region discovery] Discovered Region {discoveredRegion.Region}");
                    requestContext.ApiEvent.RegionUsed = discoveredRegion.Region;
                    requestContext.ApiEvent.AutoDetectedRegion = discoveredRegion.Region;
                    return discoveredRegion.Region;
                }
                else
                {
                    logger.Verbose(() => $"[Region discovery] {s_regionDiscoveryDetails}");
                    requestContext.ApiEvent.RegionDiscoveryFailureReason = s_regionDiscoveryDetails;
                    return null;
                }
            }

            logger.Info(() => $"[Region discovery] Returning user provided region: {azureRegionConfig}.");
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
            apiEvent.RegionAutodetectionSource = discoveredRegion.RegionSource;

            if (isAutoDiscoveryRequested)
            {
                apiEvent.RegionUsed = discoveredRegion.Region;
                apiEvent.RegionOutcome = discoveredRegion.RegionSource == RegionAutodetectionSource.FailedAutoDiscovery ?
                    RegionOutcome.FallbackToGlobal :
                    RegionOutcome.AutodetectSuccess;
            }
            else
            {
                apiEvent.RegionUsed = azureRegionConfig;
                apiEvent.RegionDiscoveryFailureReason = discoveredRegion.RegionDetails;

                if (discoveredRegion.RegionSource == RegionAutodetectionSource.FailedAutoDiscovery)
                {
                    apiEvent.RegionOutcome = RegionOutcome.UserProvidedAutodetectionFailed;
                }

                if (!string.IsNullOrEmpty(discoveredRegion.Region))
                {
                    apiEvent.RegionOutcome = string.Equals(discoveredRegion.Region, azureRegionConfig, StringComparison.OrdinalIgnoreCase) ?
                        RegionOutcome.UserProvidedValid :
                        RegionOutcome.UserProvidedInvalid;
                }
            }
        }

        private bool IsTelemetryRecorded(ApiEvent apiEvent)
        {
            return
                !(string.IsNullOrEmpty(apiEvent.RegionUsed) &&
                 apiEvent.RegionAutodetectionSource == default &&
                 apiEvent.RegionOutcome == default);
        }

        private async Task<RegionInfo> DiscoverAndCacheAsync(ILoggerAdapter logger, CancellationToken requestCancellationToken)
        {
            var regionInfo = GetCachedRegion(logger);
            if (regionInfo != null)
            {
                return regionInfo;
            }

            var result = await DiscoverAsync(logger, requestCancellationToken).ConfigureAwait(false);

            return result;
        }

        private async Task<RegionInfo> DiscoverAsync(ILoggerAdapter logger, CancellationToken requestCancellationToken)
        {
            RegionInfo result = null;
            
            await _lockDiscover.WaitAsync(requestCancellationToken).ConfigureAwait(false);
            try
            {
                var regionInfo = GetCachedRegion(logger);
                if (regionInfo != null)
                {
                    result = regionInfo;
                }
                else
                {
                    try
                    {
                        string region = Environment.GetEnvironmentVariable("REGION_NAME")?.Replace(" ", string.Empty).ToLowerInvariant();

                        if (ValidateRegion(region, "REGION_NAME env variable", logger)) // this is just to validate the region string
                        {
                            logger.Info(() => $"[Region discovery] Region found in environment variable: {region}.");
                            result = new RegionInfo(region, RegionAutodetectionSource.EnvVariable, null);
                        }
                        else
                        {
                            var headers = new Dictionary<string, string>
                                                                        {
                                                                            { "Metadata", "true" }
                                                                        };

                            Uri imdsUri = BuildImdsUri(DefaultApiVersion);

                            HttpResponse response = await _httpManager.SendGetAsync(imdsUri, headers, logger, retry: false, cancellationToken: GetCancellationToken(requestCancellationToken))
                                .ConfigureAwait(false);

                            // A bad request occurs when the version in the IMDS call is no longer supported.
                            if (response.StatusCode == HttpStatusCode.BadRequest)
                            {
                                string apiVersion = await GetImdsUriApiVersionAsync(logger, headers, requestCancellationToken).ConfigureAwait(false); // Get the latest version
                                imdsUri = BuildImdsUri(apiVersion);
                                response = await _httpManager.SendGetAsync(BuildImdsUri(apiVersion), headers, logger, retry: false, cancellationToken: GetCancellationToken(requestCancellationToken))
                                    .ConfigureAwait(false); // Call again with updated version
                            }

                            if (response.StatusCode == HttpStatusCode.OK && !response.Body.IsNullOrEmpty())
                            {
                                region = response.Body;

                                if (ValidateRegion(region, $"IMDS call to {imdsUri.AbsoluteUri}", logger))
                                {
                                    logger.Info(() => $"[Region discovery] Call to local IMDS succeeded. Region: {region}. {DateTime.UtcNow}");
                                    result = new RegionInfo(region, RegionAutodetectionSource.Imds, null);
                                }
                            }
                            else
                            {
                                s_regionDiscoveryDetails = $"Call to local IMDS failed with status code {response.StatusCode} or an empty response. {DateTime.UtcNow}";
                                logger.Error($"[Region discovery] {s_regionDiscoveryDetails}");
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        if (e is MsalServiceException msalEx && MsalError.RequestTimeout.Equals(msalEx?.ErrorCode))
                        {
                            s_regionDiscoveryDetails = $"Call to local IMDS timed out after {_imdsCallTimeoutMs}.";
                            logger.Error($"[Region discovery] {s_regionDiscoveryDetails}.");
                        }
                        else
                        {
                            s_regionDiscoveryDetails = $"IMDS call failed with exception {e}. {DateTime.UtcNow}";
                            logger.Error($"[Region discovery] {s_regionDiscoveryDetails}");
                        }
                    }
                }

                result = result ?? new RegionInfo(null, RegionAutodetectionSource.FailedAutoDiscovery, s_regionDiscoveryDetails);
            }
            finally
            {
                s_failedAutoDiscovery = result.RegionSource == RegionAutodetectionSource.FailedAutoDiscovery;
                s_autoDiscoveredRegion = result.Region;
                s_regionDiscoveryDetails = result.RegionDetails;

                _lockDiscover.Release();
            }

            return result;
        }

        // returns cached region if any.
        // if nothing is cached, returns null.
        private RegionInfo GetCachedRegion(ILoggerAdapter logger)
        {
            if (s_failedAutoDiscovery)
            {
                var autoDiscoveryError = $"[Region discovery] Auto-discovery failed in the past. Not trying again. {s_regionDiscoveryDetails}. {DateTime.UtcNow}";
                logger.Verbose(() => autoDiscoveryError);
                return new RegionInfo(null, RegionAutodetectionSource.FailedAutoDiscovery, autoDiscoveryError);
            }

            if (s_failedAutoDiscovery == false &&
                !string.IsNullOrEmpty(s_autoDiscoveredRegion))
            {
                logger.Info(() => $"[Region discovery] Auto-discovery already ran and found {s_autoDiscoveredRegion}.");
                return new RegionInfo(s_autoDiscoveredRegion, RegionAutodetectionSource.Cache, null);
            }

            logger.Verbose(() => $"[Region discovery] Auto-discovery did not run yet.");
            return null;
        }

        private static bool ValidateRegion(string region, string source, ILoggerAdapter logger)
        {
            if (string.IsNullOrEmpty(region))
            {
                logger.Verbose(() => $"[Region discovery] Region from {source} not detected. {DateTime.UtcNow}");
                return false;
            }

            if (!Uri.IsWellFormedUriString($"https://{region}.login.microsoft.com", UriKind.Absolute))
            {
                logger.Error($"[Region discovery] Region from {source} was found but it's invalid: {region}. {DateTime.UtcNow}");
                return false;
            }

            return true;
        }

        private async Task<string> GetImdsUriApiVersionAsync(ILoggerAdapter logger, Dictionary<string, string> headers, CancellationToken userCancellationToken)
        {
            Uri imdsErrorUri = new Uri(ImdsEndpoint);

            HttpResponse response = await _httpManager.SendGetAsync(imdsErrorUri, headers, logger, retry: false, cancellationToken: GetCancellationToken(userCancellationToken)).ConfigureAwait(false);

            // When IMDS endpoint is called without the api version query param, bad request response comes back with latest version.
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                LocalImdsErrorResponse errorResponse = JsonHelper.DeserializeFromJson<LocalImdsErrorResponse>(response.Body);

                if (errorResponse != null && !errorResponse.NewestVersions.IsNullOrEmpty())
                {
                    logger.Info(() => $"[Region discovery] Updated the version for IMDS endpoint to: {errorResponse.NewestVersions[0]}.");
                    return errorResponse.NewestVersions[0];
                }

                logger.Info(() => $"[Region discovery] The response is empty or does not contain the newest versions. {DateTime.UtcNow}");
            }

            logger.Info(() => $"[Region discovery] Failed to get the updated version for IMDS endpoint. HttpStatusCode: {response.StatusCode}. {DateTime.UtcNow}");

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
