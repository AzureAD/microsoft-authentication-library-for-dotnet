// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Class to initialize a managed identity and identify the service.
    /// </summary>
    internal class ManagedIdentityClient
    {
        private const string WindowsHimdsFilePath = "%Programfiles%\\AzureConnectedMachineAgent\\himds.exe";
        private const string LinuxHimdsFilePath = "/opt/azcmagent/bin/himds";

        // Preview guard: once we fall back to IMDSv1 while IMDSv2 is cached,
        // disallow switching to IMDSv2 PoP in the same process (preview behavior).
        internal static bool s_imdsV1UsedForPreview = false;
        // Non-null only after the explicit discovery API (GetManagedIdentitySourceAsync) runs.
        // Allows caching "NoneFound" (Source=None) without confusing it with "not discovered yet".
        private static ManagedIdentitySourceResult s_cachedSourceResult = null;

        // Holds the most recently minted mTLS binding certificate for this application instance.
        private X509Certificate2 _runtimeMtlsBindingCertificate;
        internal X509Certificate2 RuntimeMtlsBindingCertificate => Volatile.Read(ref _runtimeMtlsBindingCertificate);

        internal static void ResetSourceForTest()
        {
            s_cachedSourceResult = null;
            s_imdsV1UsedForPreview = false;

            // Clear cert caches so each test starts fresh
            ImdsV2ManagedIdentitySource.ResetCertCacheForTest();

            // Clear IMDS endpoint cache so environment-based endpoints are re-evaluated
            ImdsManagedIdentitySource.ResetEndpointCacheForTest();
        }

        internal async Task<(ManagedIdentityResponse Response, X509Certificate2 BindingCertificate)> SendTokenRequestForManagedIdentityAsync(
            RequestContext requestContext,
            AcquireTokenForManagedIdentityParameters parameters,
            CancellationToken cancellationToken)
        {
            AbstractManagedIdentity msi = await GetOrSelectManagedIdentitySourceAsync(requestContext, parameters.IsMtlsPopRequested, cancellationToken).ConfigureAwait(false);
            return await msi.AuthenticateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        // This method selects the managed identity source for token acquisition.
        // It does NOT probe IMDS. It uses the cached explicit discovery result if available,
        // otherwise checks environment variables, and defaults to IMDS without probing.
        private Task<AbstractManagedIdentity> GetOrSelectManagedIdentitySourceAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                requestContext.Logger.Info($"[Managed Identity] Selecting managed identity source. " + 
                    $"Discovery cached: {s_cachedSourceResult != null}");

                // Fail fast if cancellation was requested, before performing expensive network probes
                cancellationToken.ThrowIfCancellationRequested();

                ManagedIdentitySource source;

                if (s_cachedSourceResult != null)
                {
                    // Use the cached explicit discovery result (including NoneFound)
                    source = s_cachedSourceResult.Source;
                    requestContext.Logger.Info($"[Managed Identity] Using cached discovery result: {source}");
                }
                else
                {
                    // Standard path: check environment variables only, no IMDS probing
                    source = GetManagedIdentitySourceNoImds(requestContext.Logger);

                    if (source == ManagedIdentitySource.None)
                    {
                        // No environment-based source found; default to IMDS based on mTLS PoP flag
                        if (isMtlsPopRequested)
                        {
                            // Route mTLS PoP requests directly to IMDSv2 (no probing)
                            requestContext.Logger.Info("[Managed Identity] mTLS PoP requested, routing to IMDSv2 directly without probing.");
                            return Task.FromResult<AbstractManagedIdentity>(ImdsV2ManagedIdentitySource.Create(requestContext));
                        }

                        // Default to IMDSv1 without probing
                        requestContext.Logger.Info("[Managed Identity] Defaulting to IMDSv1 without probing.");
                        return Task.FromResult<AbstractManagedIdentity>(ImdsManagedIdentitySource.Create(requestContext));
                    }
                }

                // Handle NoneFound from cached discovery
                if (source == ManagedIdentitySource.None)
                {
                    throw CreateManagedIdentityUnavailableException(s_cachedSourceResult);
                }

                // Preview fallback: if ImdsV2 is cached but mTLS PoP not requested, fall back per-request to ImdsV1
                if (source == ManagedIdentitySource.ImdsV2 && !isMtlsPopRequested)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 detected, but mTLS PoP was not requested. Falling back to ImdsV1 for this request only. Please use the \"WithMtlsProofOfPossession\" API to request a token via ImdsV2.");

                    // Mark that we used IMDSv1 in this process while IMDSv2 is cached (preview behavior).
                    s_imdsV1UsedForPreview = true;

                    // Do NOT modify s_cachedSourceResult; keep cached ImdsV2 so future PoP
                    // requests can leverage it.
                    source = ManagedIdentitySource.Imds;
                }

                // Preview behavior: once we've used IMDSv1 fallback while IMDSv2 is cached,
                // we disallow switching back to IMDSv2 PoP in this process.
                if (source == ManagedIdentitySource.ImdsV2 && isMtlsPopRequested && s_imdsV1UsedForPreview)
                {
                    throw new MsalClientException(
                        MsalError.CannotSwitchBetweenImdsVersionsForPreview,
                        MsalErrorMessage.CannotSwitchBetweenImdsVersionsForPreview);
                }

                // If the source is determined to be ImdsV1 and mTLS PoP was requested,
                // throw an exception since ImdsV1 does not support mTLS PoP
                if (source == ManagedIdentitySource.Imds && isMtlsPopRequested)
                {
                    throw new MsalClientException(
                        MsalError.MtlsPopTokenNotSupportedinImdsV1,
                        MsalErrorMessage.MtlsPopTokenNotSupportedinImdsV1);
                }

                return Task.FromResult<AbstractManagedIdentity>(source switch
                {
                    ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.MachineLearning => MachineLearningManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.ImdsV2 => ImdsV2ManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.Imds => ImdsManagedIdentitySource.Create(requestContext),
                    _ => throw CreateManagedIdentityUnavailableException(s_cachedSourceResult)
                });
            }
        }

        private static ManagedIdentitySourceResult CacheDiscoveryResult(ManagedIdentitySourceResult result)
        {
            s_cachedSourceResult = result;
            return result;
        }

        // Detect managed identity source by probing IMDS endpoints.
        // This method is called only by the explicit discovery path (GetManagedIdentitySourceAsync in ManagedIdentityApplication.cs).
        // It probes IMDS v1 first, then v2 if v1 fails, and caches the result.
        internal async Task<ManagedIdentitySourceResult> GetManagedIdentitySourceAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            // Return cached result if explicit discovery already ran
            if (s_cachedSourceResult != null)
            {
                return s_cachedSourceResult;
            }

            // First check env vars to avoid the probe if possible
            ManagedIdentitySource source = GetManagedIdentitySourceNoImds(requestContext.Logger);
            
            if (source != ManagedIdentitySource.None)
            {
                return CacheDiscoveryResult(new ManagedIdentitySourceResult(source));
            }

            string imdsV1FailureReason = null;
            string imdsV2FailureReason = null;

            // Probe IMDS v1 first
            var (imdsV1Success, imdsV1Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V1, cancellationToken).ConfigureAwait(false);
            if (imdsV1Success)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV1 detected.");
                return CacheDiscoveryResult(new ManagedIdentitySourceResult(ManagedIdentitySource.Imds));
            }
            imdsV1FailureReason = imdsV1Failure;

            // If v1 fails, probe IMDS v2
            var (imdsV2Success, imdsV2Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V2, cancellationToken).ConfigureAwait(false);
            if (imdsV2Success)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV2 detected.");
                return CacheDiscoveryResult(new ManagedIdentitySourceResult(ManagedIdentitySource.ImdsV2));
            }
            imdsV2FailureReason = imdsV2Failure;

            requestContext.Logger.Info($"[Managed Identity] {MsalErrorMessage.ManagedIdentityAllSourcesUnavailable}");
            return CacheDiscoveryResult(new ManagedIdentitySourceResult(ManagedIdentitySource.None)
            {
                ImdsV1FailureReason = imdsV1FailureReason,
                ImdsV2FailureReason = imdsV2FailureReason
            });
        }

        /// <summary>
        /// Detects the managed identity source based on the availability of environment variables.
        /// It does not probe IMDS, but it checks for all other sources.
        /// This method does not cache its result, as reading environment variables is inexpensive.
        /// It is performance sensitive; any changes should be benchmarked.
        /// </summary>
        /// <param name="logger">Optional logger for diagnostic output.</param>
        /// <returns>
        /// The detected <see cref="ManagedIdentitySource"/> based on environment variables.
        /// Returns <c>ManagedIdentitySource.None</c> if no environment-based source is detected.
        /// </returns>
        internal static ManagedIdentitySource GetManagedIdentitySourceNoImds(ILoggerAdapter logger = null)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string identityHeader = EnvironmentVariables.IdentityHeader;
            string identityServerThumbprint = EnvironmentVariables.IdentityServerThumbprint;
            string msiSecret = EnvironmentVariables.IdentityHeader;
            string msiEndpoint = EnvironmentVariables.MsiEndpoint;
            string msiSecretMachineLearning = EnvironmentVariables.MsiSecret;
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;

            logger?.Info("[Managed Identity] Detecting managed identity source...");

            if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(identityHeader))
            {
                if (!string.IsNullOrEmpty(identityServerThumbprint))
                {
                    logger?.Info("[Managed Identity] Service Fabric detected.");
                    return ManagedIdentitySource.ServiceFabric;
                }
                else
                {
                    logger?.Info("[Managed Identity] App Service detected.");
                    return ManagedIdentitySource.AppService;
                }
            }
            else if (!string.IsNullOrEmpty(msiSecretMachineLearning) && !string.IsNullOrEmpty(msiEndpoint))
            {
                logger?.Info("[Managed Identity] Machine Learning detected.");
                return ManagedIdentitySource.MachineLearning;
            }
            else if (!string.IsNullOrEmpty(msiEndpoint))
            {
                logger?.Info("[Managed Identity] Cloud Shell detected.");
                return ManagedIdentitySource.CloudShell;
            }
            else if (ValidateAzureArcEnvironment(identityEndpoint, imdsEndpoint, logger))
            {
                logger?.Info("[Managed Identity] Azure Arc detected.");
                return ManagedIdentitySource.AzureArc;
            }
            else
            {
                return ManagedIdentitySource.None;
            }
        }

        // Method to return true if a file exists and is not empty to validate the Azure arc environment.
        private static bool ValidateAzureArcEnvironment(string identityEndpoint, string imdsEndpoint, ILoggerAdapter logger)
        {
            logger?.Info("[Managed Identity] Checked for sources: Service Fabric, App Service, Machine Learning, and Cloud Shell. " +
                "They are not available.");

            if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(imdsEndpoint))
            {
                logger?.Verbose(() => "[Managed Identity] Azure Arc managed identity is available through environment variables.");
                return true;
            }

            if (DesktopOsHelper.IsWindows() && File.Exists(Environment.ExpandEnvironmentVariables(WindowsHimdsFilePath)))
            {
                logger?.Verbose(() => "[Managed Identity] Azure Arc managed identity is available through file detection.");
                return true;
            }
            else if (DesktopOsHelper.IsLinux() && File.Exists(LinuxHimdsFilePath))
            {
                logger?.Verbose(() => "[Managed Identity] Azure Arc managed identity is available through file detection.");
                return true;
            }

            logger?.Verbose(() => "[Managed Identity] Azure Arc managed identity is not available.");
            return false;
        }

        /// <summary>
        /// Creates an MsalClientException for when no managed identity source is available,
        /// including detailed failure information from IMDS probes if available.
        /// </summary>
        private static MsalClientException CreateManagedIdentityUnavailableException(ManagedIdentitySourceResult sourceResult)
        {
            string errorMessage = MsalErrorMessage.ManagedIdentityAllSourcesUnavailable;

            if (sourceResult != null)
            {
                if (!string.IsNullOrEmpty(sourceResult.ImdsV1FailureReason) || !string.IsNullOrEmpty(sourceResult.ImdsV2FailureReason))
                {
                    errorMessage += " MSAL was not able to detect the Azure Instance Metadata Service (IMDS) that runs on VMs:";
                    if (!string.IsNullOrEmpty(sourceResult.ImdsV2FailureReason))
                    {
                        errorMessage += $" IMDSv2: {sourceResult.ImdsV2FailureReason}.";
                    }
                    if (!string.IsNullOrEmpty(sourceResult.ImdsV1FailureReason))
                    {
                        errorMessage += $" IMDSv1: {sourceResult.ImdsV1FailureReason}.";
                    }
                }
            }

            return new MsalClientException(MsalError.ManagedIdentityAllSourcesUnavailable, errorMessage);
        }

        /// <summary>
        /// Sets (or replaces) the in-memory binding certificate used to prime the mtls_pop scheme on subsequent requests.
        /// The certificate is intentionally NOT disposed here to avoid invalidating caller-held references (e.g., via AuthenticationResult).
        /// </summary>
        /// <remarks>
        /// Lifetime considerations:
        /// - The binding certificate is ephemeral and valid for the token's binding duration.
        /// - If rotation occurs, older certificates will be eligible for GC once no longer referenced.
        /// - Explicit disposal can be revisited if a deterministic rotation / shutdown strategy is introduced.
        /// </remarks>
        internal void SetRuntimeMtlsBindingCertificate(X509Certificate2 cert)
        {
            Volatile.Write(ref _runtimeMtlsBindingCertificate, cert);
        }
    }
}
