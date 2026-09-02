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
        //
        // The result and the kill switch state it was computed under are stored together in a single
        // immutable snapshot, published and read through one volatile reference. Keeping them in two
        // separate fields would let a reader observe a fresh result paired with a stale flag (or the
        // reverse) and misjudge whether the cached value describes the host or merely describes the
        // switch, so the pair is made untearable by construction rather than by argument.
        private static CachedDiscovery s_cachedDiscovery;

        /// <summary>
        /// A discovery result together with the state of the IMDSv2 kill switch when it was computed.
        /// </summary>
        /// <remarks>
        /// A result produced while the switch was set never probed IMDSv2, so it records the switch's
        /// effect rather than the host's capability and must be discarded once the switch clears. The
        /// reverse transition needs no re-probe: a result captured with the switch off holds the true
        /// capability and is masked on read. Tracking the state keeps discovery O(1) in both modes,
        /// which matters because consumers such as Azure.Identity call discovery on every
        /// authentication and rely on this cache to make that free.
        /// </remarks>
        private sealed class CachedDiscovery
        {
            internal CachedDiscovery(ManagedIdentityDiscoveryResult result, bool capturedWhileImdsV2Disabled)
            {
                Result = result;
                CapturedWhileImdsV2Disabled = capturedWhileImdsV2Disabled;
            }

            internal ManagedIdentityDiscoveryResult Result { get; }

            internal bool CapturedWhileImdsV2Disabled { get; }

            /// <summary>
            /// True when this snapshot cannot answer for the host under the supplied switch state,
            /// which happens only when the switch has been cleared since the result was computed.
            /// </summary>
            internal bool IsStaleUnder(bool imdsV2Disabled) => CapturedWhileImdsV2Disabled && !imdsV2Disabled;
        }

        // Set once the "variable is set to an unrecognized value" warning has been emitted, so a
        // misconfiguration is reported without repeating on every discovery call.
        private static int s_unrecognizedImdsV2DisableValueLogged;

        // Serializes explicit capability discovery so concurrent callers at process startup do not
        // issue redundant IMDS probes or provision the binding key more than once.
        private static readonly SemaphoreSlim s_discoveryLock = new SemaphoreSlim(1, 1);

        // Holds the most recently minted mTLS binding certificate for this application instance.
        private X509Certificate2 _runtimeMtlsBindingCertificate;
        internal X509Certificate2 RuntimeMtlsBindingCertificate => Volatile.Read(ref _runtimeMtlsBindingCertificate);

        internal static void ResetSourceForTest()
        {
            Volatile.Write(ref s_cachedDiscovery, null);
            s_unrecognizedImdsV2DisableValueLogged = 0;

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
            AbstractManagedIdentity msi = await GetOrSelectManagedIdentitySourceAsync(requestContext, parameters.IsMtlsPopRequested || parameters.PreferMsiV2, cancellationToken).ConfigureAwait(false);
            return await msi.AuthenticateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Mints (or reuses) the IMDSv2 mTLS binding without sending the token request, so the caller
        /// can delegate the token leg to MSAL's internal exchange path (<see cref="OAuth2.TokenClient"/>).
        /// mTLS PoP always routes to the IMDSv2 source.
        /// </summary>
        internal async Task<MtlsBindingInfo> AcquireImdsV2MtlsBindingAsync(
            RequestContext requestContext,
            AcquireTokenForManagedIdentityParameters parameters,
            bool forceRemint,
            CancellationToken cancellationToken)
        {
            // Kill switch gate. This must run before anything else in this method: the min-strength
            // check below calls explicit discovery, which probes IMDS and can provision a binding key,
            // and the binding acquisition further down can mint or reuse a cached certificate. Failing
            // here guarantees no IMDSv2 HTTP, CSR, certificate, or key-provisioning work happens while
            // the switch is set, including when a warm certificate cache would otherwise skip the probes.
            //
            // Scoped to hosts that would actually route to IMDS: on an environment-detected source
            // (App Service, Service Fabric, Cloud Shell, Azure Arc, Machine Learning) IMDSv2 is never
            // involved, so those hosts keep their existing, more precise MtlsPopNotSupportedForEnvironment
            // error rather than being told IMDSv2 is disabled.
            if (EnvironmentVariables.IsImdsV2Disabled &&
                GetManagedIdentitySourceNoImds(requestContext.Logger) == ManagedIdentitySource.None)
            {
                requestContext.Logger.Warning(
                    "[Managed Identity] IMDSv2 is disabled via the " + EnvironmentVariables.DisableImdsV2EnvVar +
                    " environment variable; blocking the mTLS binding request before any certificate or key operation.");

                throw new MsalClientException(MsalError.ImdsV2Disabled, MsalErrorMessage.ImdsV2Disabled);
            }

            // Enforce a minimum binding strength floor when requested via PoPOptions.MinStrength.
            // The mTLS PoP token leg is delegated to the internal TokenClient exchange and never probes
            // for binding strength on its own, so explicitly run discovery to learn the host's maximum
            // binding strength and fail fast if the host cannot meet the required floor before minting.
            if (parameters.MtlsPopMinStrength > MtlsBindingStrength.None)
            {
                ManagedIdentityDiscoveryResult discovery =
                    await GetManagedIdentityCapabilitiesAsync(requestContext, cancellationToken).ConfigureAwait(false);

                if (discovery.MaxSupportedBindingStrength < parameters.MtlsPopMinStrength)
                {
                    throw new MsalClientException(
                        MsalError.MinStrengthNotMet,
                        MsalErrorMessage.MinStrengthNotMet(discovery.MaxSupportedBindingStrength, parameters.MtlsPopMinStrength));
                }
            }

            // Route through the shared source decision so the same guards apply as the bearer path
            // (e.g. throwing MtlsPopTokenNotSupportedinImdsV1 when only IMDSv1 is available). mTLS PoP
            // always resolves to the IMDSv2 source.
            (ManagedIdentitySource source, bool isImdsV2) = SelectManagedIdentitySourceType(
                requestContext, isMtlsPopRequested: true, cancellationToken);

            // An environment-detected source (App Service, Service Fabric, Cloud Shell, Azure Arc,
            // Machine Learning) does not support mTLS PoP. Fail fast with a clear MSAL error.
            if (source != ManagedIdentitySource.Imds || !isImdsV2)
            {
                throw new MsalClientException(
                    MsalError.MtlsPopNotSupportedForEnvironment,
                    MsalErrorMessage.MtlsPopNotSupportedForManagedIdentityEnvironmentMessage);
            }

            IImdsV2MtlsBindingSource imdsV2Source = ImdsV2ManagedIdentitySource.Create(requestContext);

            return await imdsV2Source
                .AcquireMtlsBindingForDelegationAsync(parameters, forceRemint, cancellationToken)
                .ConfigureAwait(false);
        }

        // This method selects and instantiates the managed identity source for the bearer token
        // path. IMDSv2 (mTLS PoP) does not derive from AbstractManagedIdentity and is created via
        // AcquireImdsV2MtlsBindingAsync, so the IMDS source here always resolves to IMDSv1.
        private Task<AbstractManagedIdentity> GetOrSelectManagedIdentitySourceAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            (ManagedIdentitySource source, _) = SelectManagedIdentitySourceType(
                requestContext, isMtlsPopRequested, cancellationToken);

            return Task.FromResult<AbstractManagedIdentity>(source switch
            {
                ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.MachineLearning => MachineLearningManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.Imds => ImdsManagedIdentitySource.Create(requestContext),
                _ => throw CreateManagedIdentityUnavailableException(Volatile.Read(ref s_cachedDiscovery)?.Result)
            });
        }

        // Decides the managed identity source (and IMDS version) without instantiating it.
        // It does NOT probe IMDS. It uses the cached explicit discovery result if available,
        // otherwise checks environment variables, and defaults to IMDS without probing.
        private (ManagedIdentitySource Source, bool IsImdsV2) SelectManagedIdentitySourceType(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                requestContext.Logger.Info($"[Managed Identity] Selecting managed identity source. " + 
                    $"Discovery cached: {Volatile.Read(ref s_cachedDiscovery) != null}");

                // Fail fast if cancellation was requested, before performing expensive network probes
                cancellationToken.ThrowIfCancellationRequested();

                // Read the kill switch once per selection. It is deliberately evaluated here, at the
                // routing decision, rather than only during discovery: discovery results are cached in a
                // process-wide static that never expires, so a process that cached "IMDSv2" before the
                // switch was set would otherwise route straight past the switch.
                bool imdsV2Disabled = EnvironmentVariables.IsImdsV2Disabled;

                // Also warn here, not only in discovery. A process that only ever calls
                // AcquireTokenForManagedIdentity for bearer tokens never runs capability discovery,
                // and that is exactly the process most likely to be running during an IMDSv2
                // incident - so it is the one that most needs to be told its mitigation is inert.
                WarnOnceIfImdsV2DisableValueUnrecognized(requestContext);

                ManagedIdentitySource source;
                bool isImdsV2 = false;

                // A discovery result captured while the switch was set says "IMDSv1, no binding"
                // because the IMDSv2 probe was skipped, not because the host is incapable. Once the
                // switch clears, treat it as absent so routing re-derives the source instead of
                // latching the mitigation's effect past its lifetime. One volatile read gives a
                // consistent result/switch-state pair.
                CachedDiscovery snapshot = Volatile.Read(ref s_cachedDiscovery);
                ManagedIdentityDiscoveryResult cachedResult =
                    (snapshot is null || snapshot.IsStaleUnder(imdsV2Disabled)) ? null : snapshot.Result;

                if (cachedResult != null)
                {
                    // Use the cached explicit discovery result (including NoneFound)
                    source = cachedResult.Source;
                    isImdsV2 = cachedResult.DetectedImdsVersion == ImdsVersion.V2;
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
                            if (imdsV2Disabled)
                            {
                                requestContext.Logger.Warning(
                                    "[Managed Identity] IMDSv2 is disabled via the " + EnvironmentVariables.DisableImdsV2EnvVar +
                                    " environment variable; an mTLS request cannot be served over IMDSv1.");

                                throw new MsalClientException(MsalError.ImdsV2Disabled, MsalErrorMessage.ImdsV2Disabled);
                            }

                            // Route mTLS PoP requests directly to IMDSv2 (no probing)
                            requestContext.Logger.Info("[Managed Identity] mTLS PoP requested, routing to IMDSv2 directly without probing.");
                            return (ManagedIdentitySource.Imds, true);
                        }

                        // Default to IMDSv1 without probing
                        requestContext.Logger.Info("[Managed Identity] Defaulting to IMDSv1 without probing.");
                        return (ManagedIdentitySource.Imds, false);
                    }
                }

                // Handle NoneFound from cached discovery
                if (source == ManagedIdentitySource.None)
                {
                    throw CreateManagedIdentityUnavailableException(cachedResult);
                }

                // Kill switch: a cached IMDSv2 discovery result must not survive the switch being set.
                // Downgrade to IMDSv1 for routing purposes without mutating s_cachedSourceResult, so the
                // cached discovery stays valid if the switch is later cleared. Bearer requests continue
                // over IMDSv1; mTLS requests fall through to the guard below and fail fast.
                if (imdsV2Disabled && isImdsV2)
                {
                    requestContext.Logger.Info(
                        "[Managed Identity] IMDSv2 was detected but is disabled via the " + EnvironmentVariables.DisableImdsV2EnvVar +
                        " environment variable; routing this request over IMDSv1.");

                    isImdsV2 = false;
                }

                // Per-request fallback: if ImdsV2 is cached but mTLS PoP not requested, use ImdsV1 for this request only.
                // We do NOT latch this state; future PoP requests can still leverage the cached ImdsV2 discovery.
                if (isImdsV2 && !isMtlsPopRequested)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 detected, but neither mTLS PoP nor mTLS Bearer requested. Using IMDSv1 for this request only. Please use the \"WithMtlsProofOfPossession\" or \"WithRequestOverMtls\" API to request a token via ImdsV2.");

                    // Do NOT modify s_cachedSourceResult; keep cached ImdsV2 so future PoP
                    // requests can leverage it. Route this request through IMDSv1 only.
                    isImdsV2 = false;
                }

                // If the source is determined to be ImdsV1 and mTLS PoP was requested,
                // throw an exception since ImdsV1 does not support mTLS PoP. When the kill switch is
                // what forced IMDSv1, report that instead: "the operator disabled IMDSv2" is a different
                // and more actionable problem than "this host cannot do IMDSv2".
                if (source == ManagedIdentitySource.Imds && !isImdsV2 && isMtlsPopRequested)
                {
                    if (imdsV2Disabled)
                    {
                        throw new MsalClientException(MsalError.ImdsV2Disabled, MsalErrorMessage.ImdsV2Disabled);
                    }

                    throw new MsalClientException(
                        MsalError.MtlsPopTokenNotSupportedinImdsV1,
                        MsalErrorMessage.MtlsPopTokenNotSupportedinImdsV1);
                }

                return (source, isImdsV2);
            }
        }

        private static ManagedIdentityDiscoveryResult CacheDiscoveryResult(
            ManagedIdentityDiscoveryResult result,
            bool imdsV2Disabled)
        {
            // Single volatile publication of an immutable pair, so no reader can see the result
            // without also seeing the switch state it was computed under.
            Volatile.Write(ref s_cachedDiscovery, new CachedDiscovery(result, imdsV2Disabled));
            return result;
        }

        /// <summary>
        /// Logs once per process when the kill switch variable is set to a value MSAL does not
        /// recognize, so an operator is not left believing an inactive mitigation is active.
        /// </summary>
        private static void WarnOnceIfImdsV2DisableValueUnrecognized(RequestContext requestContext)
        {
            if (!EnvironmentVariables.HasUnrecognizedImdsV2DisableValue ||
                Interlocked.Exchange(ref s_unrecognizedImdsV2DisableValueLogged, 1) != 0)
            {
                return;
            }

            requestContext.Logger.Warning(
                "[Managed Identity] The " + EnvironmentVariables.DisableImdsV2EnvVar +
                " environment variable is set to an unrecognized value and is being ignored; " +
                "IMDSv2 remains enabled. Set it to exactly \"true\" or \"1\" to disable IMDSv2.");
        }

        /// <summary>
        /// Returns the cached discovery result if it is still usable under the current kill switch
        /// state, projected through <see cref="ApplyImdsV2KillSwitch"/>.
        /// </summary>
        /// <remarks>
        /// A result cached while the switch was set never ran the IMDSv2 probe, so it cannot tell us
        /// what the host is capable of once the switch clears; that direction must re-probe, which is
        /// what keeps the mitigation reversible in place. Every other combination is served from the
        /// cache, so an active mitigation does not turn each discovery call into an IMDS round trip.
        /// </remarks>
        private static bool TryGetUsableCachedResult(
            bool imdsV2Disabled,
            RequestContext requestContext,
            out ManagedIdentityDiscoveryResult result)
        {
            CachedDiscovery snapshot = Volatile.Read(ref s_cachedDiscovery);

            if (snapshot is null || snapshot.IsStaleUnder(imdsV2Disabled))
            {
                result = null;
                return false;
            }

            result = ApplyImdsV2KillSwitch(snapshot.Result, imdsV2Disabled, requestContext);
            return true;
        }

        /// <summary>
        /// Projects a discovery result through the IMDSv2 kill switch.
        /// </summary>
        /// <remarks>
        /// Applied on every read rather than baked into the cached value. The cached result records
        /// what the host is actually capable of; the switch is an operator override layered on top at
        /// the point of use. Masking on read means clearing the switch restores the true capability
        /// immediately, with no re-probe and no process restart, and it closes the window where a
        /// process that cached "IMDSv2 / KeyGuard" before the switch was set would keep advertising
        /// PoP support that MSAL will now refuse to honor.
        /// </remarks>
        private static ManagedIdentityDiscoveryResult ApplyImdsV2KillSwitch(
            ManagedIdentityDiscoveryResult result,
            bool imdsV2Disabled,
            RequestContext requestContext)
        {
            // Only the IMDS path can involve IMDSv2. Environment-detected sources (App Service,
            // Service Fabric, Cloud Shell, Azure Arc, Machine Learning) are reported unchanged.
            if (!imdsV2Disabled ||
                result.Source != ManagedIdentitySource.Imds ||
                (result.DetectedImdsVersion != ImdsVersion.V2 &&
                 result.MaxSupportedBindingStrength == MtlsBindingStrength.None))
            {
                return result;
            }

            requestContext.Logger.Info(
                "[Managed Identity] Reporting no mTLS binding capability because IMDSv2 is disabled via the " +
                EnvironmentVariables.DisableImdsV2EnvVar + " environment variable.");

            return new ManagedIdentityDiscoveryResult(
                ManagedIdentitySource.Imds,
                ImdsVersion.V1,
                MtlsBindingStrength.None,
                imdsV1FailureReason: result.ImdsV1FailureReason,
                imdsV2FailureReason: MsalErrorMessage.ImdsV2DisabledDiscoveryReason);
        }

        // Detect managed identity source by probing IMDS endpoints.
        // This method is called only by the explicit discovery path (GetManagedIdentityCapabilitiesAsync in ManagedIdentityApplication.cs).
        // It probes IMDS v2 first, then v1 if v2 fails, and caches the result.
        internal async Task<ManagedIdentityDiscoveryResult> GetManagedIdentityCapabilitiesAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken)
        {
            bool imdsV2Disabled = EnvironmentVariables.IsImdsV2Disabled;

            WarnOnceIfImdsV2DisableValueUnrecognized(requestContext);

            // Fast path: explicit discovery already completed.
            if (TryGetUsableCachedResult(imdsV2Disabled, requestContext, out ManagedIdentityDiscoveryResult cached))
            {
                return cached;
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
                if (TryGetUsableCachedResult(imdsV2Disabled, requestContext, out ManagedIdentityDiscoveryResult cachedUnderLock))
                {
                    return cachedUnderLock;
                }

                // First check env vars to avoid the probe if possible
                ManagedIdentitySource source = GetManagedIdentitySourceNoImds(requestContext.Logger);

                if (source != ManagedIdentitySource.None)
                {
                    return CacheDiscoveryResult(new ManagedIdentityDiscoveryResult(source), imdsV2Disabled);
                }

                string imdsV1FailureReason = null;
                string imdsV2FailureReason = null;

                // Kill switch: skip the IMDSv2 probe entirely and go straight to IMDSv1, so no IMDSv2
                // HTTP request or key provisioning happens while the switch is set. The reason is
                // recorded so it surfaces on ManagedIdentityCapabilities.ErrorReason and explains why
                // the reported binding strength is None. Read once at the top of this method so a
                // single discovery pass cannot observe the switch changing halfway through.
                if (imdsV2Disabled)
                {
                    requestContext.Logger.Info(
                        "[Managed Identity] IMDSv2 is disabled via the " + EnvironmentVariables.DisableImdsV2EnvVar +
                        " environment variable; skipping the IMDSv2 probe and probing IMDSv1 only.");

                    imdsV2FailureReason = MsalErrorMessage.ImdsV2DisabledDiscoveryReason;
                }
                else
                {
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
                            v2Strength), imdsV2Disabled);
                    }
                    imdsV2FailureReason = imdsV2Failure;
                }

                // If v2 fails, fall back to probing IMDS v1.
                var (imdsV1Success, imdsV1Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V1, cancellationToken).ConfigureAwait(false);
                if (imdsV1Success)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV1 detected.");

                    // While IMDSv2 is disabled no request can be bound to a key, so report None rather
                    // than the host's theoretical capability. Advertising a binding strength MSAL will
                    // refuse to honor would send credential chains (such as DefaultAzureCredential) down
                    // a PoP path that is guaranteed to fail. Determining the strength is skipped
                    // entirely, since the answer is fixed.
                    MtlsBindingStrength strength = imdsV2Disabled
                        ? MtlsBindingStrength.None
                        : await DetermineImdsV1BindingStrengthAsync(requestContext, cancellationToken).ConfigureAwait(false);
                    requestContext.Logger.Info($"[Managed Identity] Host max supported binding strength: {strength}.");

                    return CacheDiscoveryResult(new ManagedIdentityDiscoveryResult(
                        ManagedIdentitySource.Imds,
                        ImdsVersion.V1,
                        strength,
                        imdsV2FailureReason: imdsV2Disabled ? imdsV2FailureReason : null), imdsV2Disabled);
                }
                imdsV1FailureReason = imdsV1Failure;

                requestContext.Logger.Info($"[Managed Identity] {MsalErrorMessage.ManagedIdentityAllSourcesUnavailable}");
                return CacheDiscoveryResult(new ManagedIdentityDiscoveryResult(
                    ManagedIdentitySource.None,
                    imdsV1FailureReason: imdsV1FailureReason,
                    imdsV2FailureReason: imdsV2FailureReason), imdsV2Disabled);
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
