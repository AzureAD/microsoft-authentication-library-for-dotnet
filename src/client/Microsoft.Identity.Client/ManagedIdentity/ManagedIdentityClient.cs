// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.AppConfig;
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

        // Non-null only after the explicit discovery API (GetManagedIdentityCapabilitiesAsync) runs.
        // Allows caching "NoneFound" (Source=None) without confusing it with "not discovered yet".
        private static ManagedIdentityDiscoveryResult s_cachedSourceResult = null;

        // Serializes explicit capability discovery so concurrent callers at process startup do not
        // issue redundant IMDS probes or provision the binding key more than once.
        private static readonly SemaphoreSlim s_discoveryLock = new SemaphoreSlim(1, 1);

        // Holds the most recently minted mTLS binding certificate for this application instance.
        private X509Certificate2 _runtimeMtlsBindingCertificate;
        internal X509Certificate2 RuntimeMtlsBindingCertificate => Volatile.Read(ref _runtimeMtlsBindingCertificate);

        internal static void ResetSourceForTest()
        {
            s_cachedSourceResult = null;

            // Clear cert caches so each test starts fresh
            ImdsV2ManagedIdentitySource.ResetCertCacheForTest();

            // Clear IMDS endpoint cache so environment-based endpoints are re-evaluated
            ImdsManagedIdentitySource.ResetEndpointCacheForTest();
        }

        internal async Task<ManagedIdentityResponse> SendTokenRequestForManagedIdentityAsync(
            RequestContext requestContext,
            AcquireTokenForManagedIdentityParameters parameters,
            CancellationToken cancellationToken)
        {
            AbstractManagedIdentity msi = await GetOrSelectManagedIdentitySourceAsync(requestContext, parameters.IsMtlsPopRequested || parameters.IsMtlsBearerRequested, cancellationToken).ConfigureAwait(false);
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
                bool isImdsV2 = false;

                if (s_cachedSourceResult != null)
                {
                    // Use the cached explicit discovery result (including NoneFound)
                    source = s_cachedSourceResult.Source;
                    isImdsV2 = s_cachedSourceResult.DetectedImdsVersion == ImdsVersion.V2;
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

                // Per-request fallback: if ImdsV2 is cached but mTLS PoP not requested, use ImdsV1 for this request only.
                // We do NOT latch this state; future PoP requests can still leverage the cached ImdsV2 discovery.
                if (isImdsV2 && !isMtlsPopRequested)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 detected, but neither mTLS PoP nor mTLS ****** requested. Using ImdsV1 for this request only. Please use the \"WithMtlsProofOfPossession\" or \"WithMtlsBearerToken\" API to request a token via ImdsV2.");

                    // Do NOT modify s_cachedSourceResult; keep cached ImdsV2 so future PoP
                    // requests can leverage it. Route this request through IMDSv1 only.
                    isImdsV2 = false;
                }

                // If the source is determined to be ImdsV1 and mTLS PoP was requested,
                // throw an exception since ImdsV1 does not support mTLS PoP
                if (source == ManagedIdentitySource.Imds && !isImdsV2 && isMtlsPopRequested)
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
                    ManagedIdentitySource.Imds => isImdsV2
                        ? ImdsV2ManagedIdentitySource.Create(requestContext)
                        : ImdsManagedIdentitySource.Create(requestContext),
                    _ => throw CreateManagedIdentityUnavailableException(s_cachedSourceResult)
                });
            }
        }

        private static ManagedIdentityDiscoveryResult CacheDiscoveryResult(ManagedIdentityDiscoveryResult result)
        {
            s_cachedSourceResult = result;
            return result;
        }

        // Detect managed identity source by probing IMDS endpoints.
        // This method is called only by the explicit discovery path (GetManagedIdentityCapabilitiesAsync in ManagedIdentityApplication.cs).
        // It probes IMDS v2 first, then v1 if v2 fails, and caches the result.
        internal async Task<ManagedIdentityDiscoveryResult> GetManagedIdentityCapabilitiesAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            // Fast path: explicit discovery already completed.
            if (s_cachedSourceResult != null)
            {
                return s_cachedSourceResult;
            }

            // Single-flight: ensure only one caller probes IMDS / provisions a binding key at a
            // time. Concurrent callers at process startup wait here and then observe the cached
            // result instead of issuing redundant probes. Try a non-blocking acquire first so an
            // uncontended caller keeps the existing cancellation point (the HTTP probe); only a
            // contended caller waits, and that wait is cancelable.
            bool lockTaken = s_discoveryLock.Wait(0);
            if (!lockTaken)
            {
                await s_discoveryLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                lockTaken = true;
            }

            try
            {
                // Re-check under the lock in case another caller completed discovery while we waited.
                if (s_cachedSourceResult != null)
                {
                    return s_cachedSourceResult;
                }

                // First check env vars to avoid the probe if possible
                ManagedIdentitySource source = GetManagedIdentitySourceNoImds(requestContext.Logger);

                if (source != ManagedIdentitySource.None)
                {
                    return CacheDiscoveryResult(new ManagedIdentityDiscoveryResult(source));
                }

                string imdsV1FailureReason = null;
                string imdsV2FailureReason = null;

                // Probe IMDS v2 first. The v2 path (CSR metadata endpoint) only exists on hosts that
                // actually support IMDSv2; on v1-only hosts it returns 404. Probing v2 first avoids
                // the v1 success-on-400 contract masking a v2-capable host (see issue #6024).
                var (imdsV2Success, imdsV2Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V2, cancellationToken).ConfigureAwait(false);
                if (imdsV2Success)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 detected.");

                    // A successful IMDSv2 probe proves the host speaks the key-bound CSR (PoP) protocol,
                    // so it can bind at least at Software strength. Probe the platform key provider to see
                    // whether it can produce a VBS-isolated KeyGuard key and thus advertise the stronger,
                    // attested KeyGuard tier. The v2 PoP token flow itself requires a KeyGuard key, so this
                    // mirrors what an actual PoP request would obtain.
                    MtlsBindingStrength v2Strength = await DetermineImdsV2BindingStrengthAsync(requestContext, cancellationToken).ConfigureAwait(false);
                    requestContext.Logger.Info($"[Managed Identity] Host max supported binding strength: {v2Strength}.");

                    return CacheDiscoveryResult(new ManagedIdentityDiscoveryResult(
                        ManagedIdentitySource.Imds,
                        ImdsVersion.V2,
                        v2Strength));
                }
                imdsV2FailureReason = imdsV2Failure;

                // If v2 fails, fall back to probing IMDS v1.
                var (imdsV1Success, imdsV1Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V1, cancellationToken).ConfigureAwait(false);
                if (imdsV1Success)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV1 detected.");

                    MtlsBindingStrength strength = await DetermineImdsV1BindingStrengthAsync(requestContext, cancellationToken).ConfigureAwait(false);
                    requestContext.Logger.Info($"[Managed Identity] Host max supported binding strength: {strength}.");

                    return CacheDiscoveryResult(new ManagedIdentityDiscoveryResult(
                        ManagedIdentitySource.Imds,
                        ImdsVersion.V1,
                        strength));
                }
                imdsV1FailureReason = imdsV1Failure;

                requestContext.Logger.Info($"[Managed Identity] {MsalErrorMessage.ManagedIdentityAllSourcesUnavailable}");
                return CacheDiscoveryResult(new ManagedIdentityDiscoveryResult(
                    ManagedIdentitySource.None,
                    imdsV1FailureReason: imdsV1FailureReason,
                    imdsV2FailureReason: imdsV2FailureReason));
            }
            finally
            {
                if (lockTaken)
                {
                    s_discoveryLock.Release();
                }
            }
        }

        // Determines the host's maximum mTLS binding strength for IMDSv1-only hosts using the
        // /metadata/instance/compute security profile. mTLS PoP is not supported on .NET
        // Framework 4.6.2, so the host is reported as None there.
        private static Task<MtlsBindingStrength> DetermineImdsV1BindingStrengthAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
#if NET462
            return Task.FromResult(MtlsBindingStrength.None);
#else
            return DetermineImdsV1BindingStrengthCoreAsync(requestContext, cancellationToken);
#endif
        }

#if !NET462
        private static async Task<MtlsBindingStrength> DetermineImdsV1BindingStrengthCoreAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            ComputeMetadataResponse computeMetadata = await ImdsComputeMetadataManager.GetComputeMetadataAsync(
                requestContext.ServiceBundle.HttpManager,
                requestContext.Logger,
                cancellationToken).ConfigureAwait(false);

            // A Windows TVM/CVM security profile indicates key-binding capability. We report
            // Software (binding available) rather than KeyGuard: the security profile alone does
            // not prove a successful VBS/KeyGuard attestation, and an IMDSv1-only host cannot use
            // the v2 CSR (PoP) flow regardless, so we must not overclaim attestation.
            return ImdsComputeMetadataManager.IsMtlsPopSupported(computeMetadata)
                ? MtlsBindingStrength.Software
                : MtlsBindingStrength.None;
        }
#endif

        // Determines the IMDSv2 host's maximum mTLS binding strength. The host supports at least
        // Software binding (the v2 CSR flow binds a token to a key); if the platform can produce a
        // VBS-isolated KeyGuard key it supports the stronger, attested KeyGuard tier. mTLS PoP is
        // unavailable on .NET Framework 4.6.2, so the host is reported as None there.
        private static Task<MtlsBindingStrength> DetermineImdsV2BindingStrengthAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
#if NET462
            return Task.FromResult(MtlsBindingStrength.None);
#else
            return DetermineImdsV2BindingStrengthCoreAsync(requestContext, cancellationToken);
#endif
        }

#if !NET462
        private static async Task<MtlsBindingStrength> DetermineImdsV2BindingStrengthCoreAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            ManagedIdentityKeyType keyType;
            try
            {
                IManagedIdentityKeyProvider keyProvider = requestContext.ServiceBundle.PlatformProxy.ManagedIdentityKeyProvider;
                ManagedIdentityKeyInfo keyInfo = await keyProvider
                    .GetOrCreateKeyAsync(requestContext.Logger, cancellationToken)
                    .ConfigureAwait(false);
                keyType = keyInfo.Type;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Failing to obtain a key does not invalidate the host's v2/Software capability;
                // keep the Software floor rather than failing capability discovery.
                requestContext.Logger.Info($"[Managed Identity] KeyGuard probe failed; reporting Software binding strength. {ex.Message}");
                return MtlsBindingStrength.Software;
            }

            // Only a VBS-isolated KeyGuard key justifies the attested KeyGuard tier. Any other key
            // type stays at the Software floor; we never downgrade a confirmed v2 host below Software.
            return keyType == ManagedIdentityKeyType.KeyGuard
                ? MtlsBindingStrength.KeyGuard
                : MtlsBindingStrength.Software;
        }
#endif

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
        private static MsalClientException CreateManagedIdentityUnavailableException(ManagedIdentityDiscoveryResult discoveryResult)
        {
            string errorMessage = MsalErrorMessage.ManagedIdentityAllSourcesUnavailable;

            string combinedReason = discoveryResult?.GetCombinedErrorReason();
            if (!string.IsNullOrEmpty(combinedReason))
            {
                errorMessage += " The Azure Instance Metadata Service (IMDS) that runs on VMs was not detected: " + combinedReason;
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
