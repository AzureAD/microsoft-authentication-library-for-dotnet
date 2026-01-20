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
    /// Managed Identity source selection and IMDS probing.
    ///
    /// This class is responsible for:
    ///  1) Determining which MI "source" applies (AppService / ServiceFabric / Arc / IMDS etc.)
    ///  2) Probing IMDS endpoints when explicitly requested (probe==true), OR when needed for PoP
    ///  3) Enforcing policy rules (e.g., PoP requires IMDSv2)
    ///  4) Constructing the corresponding <see cref="AbstractManagedIdentity"/> implementation.
    ///
    /// Important behavior rules (kept intentionally explicit in code):
    ///  - Default selection is NO-PROBE (perf): "env-based sources first, else DefaultToImds sentinel"
    ///  - Default selection is also tied to Azure SDKs DefaultAzureCredential behavior.
    ///  - PoP requests implicitly probe IMDSv2 even when probe == false (needed to find IMDSv2 preview)
    ///  - PoP does NOT probe IMDSv1 (because v1 can never satisfy a PoP request)
    ///  - probe == true probes BOTH IMDSv2 and IMDSv1 (diagnostics + v1-only environments like AKS)
    ///  - probe for v1 IMDS when needed only (no unnecessary network calls)
    ///  - Azure SDK will probe IMDS v1 when ManagedIdentityCredential is constructed explicitly
    ///
    /// Caching:
    ///  - s_sourceName is a process-wide, best-effort cache (no locks, by existing design).
    ///  - Once IMDSv2 is detected, we try hard not to overwrite it with v1-ish states (DefaultToImds/Imds),
    ///    so subsequent PoP calls can still leverage v2 capability.
    /// </summary>
    internal class ManagedIdentityClient
    {
        private const string WindowsHimdsFilePath = "%Programfiles%\\AzureConnectedMachineAgent\\himds.exe";
        private const string LinuxHimdsFilePath = "/opt/azcmagent/bin/himds";

        /// <summary>
        /// Process-wide cached "best known" MI source.
        /// This is intentionally static.
        /// </summary>
        internal static ManagedIdentitySource s_sourceName = ManagedIdentitySource.None;

        /// <summary>
        /// Preview guard: once we served a non-PoP request via IMDSv1 while IMDSv2 is cached,
        /// disallow switching back to IMDSv2 PoP in the same process (preview behavior).
        /// </summary>
        internal static bool s_imdsV1UsedForPreview = false;

        /// <summary>
        /// Holds the most recently minted mTLS binding certificate for this application instance.
        /// Used to prime mtls_pop subsequent requests.
        /// </summary>
        private X509Certificate2 _runtimeMtlsBindingCertificate;

        internal X509Certificate2 RuntimeMtlsBindingCertificate => Volatile.Read(ref _runtimeMtlsBindingCertificate);

        internal static void ResetSourceForTest()
        {
            s_sourceName = ManagedIdentitySource.None;
            s_imdsV1UsedForPreview = false;

            // Clear cert caches so each test starts fresh
            ImdsV2ManagedIdentitySource.ResetCertCacheForTest();
        }

        internal async Task<ManagedIdentityResponse> SendTokenRequestForManagedIdentityAsync(
            RequestContext requestContext,
            AcquireTokenForManagedIdentityParameters parameters,
            CancellationToken cancellationToken)
        {
            // Token acquisition is always "select source -> enforce rules -> create -> authenticate".
            AbstractManagedIdentity msi =
                await GetOrSelectManagedIdentitySourceAsync(
                        requestContext,
                        parameters.IsMtlsPopRequested,
                        cancellationToken)
                    .ConfigureAwait(false);

            return await msi.AuthenticateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Token request path:
        ///  - Detect a source (cached if possible)
        ///  - Apply per-request IMDS fallback policy (v2 -> v1 for non-PoP requests)
        ///  - Enforce policy constraints (PoP requires v2, preview switching rules)
        ///  - Create and return the correct MI source implementation
        /// </summary>
        private async Task<AbstractManagedIdentity> GetOrSelectManagedIdentitySourceAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                requestContext.Logger.Info(
                    $"[Managed Identity] Selecting managed identity source if not cached. Cached value is {s_sourceName} ");

                // 1) Detect which source we should use for THIS request (cache-aware).
                //    Note: detection is "no-probe" by default for perf. PoP may still implicitly probe v2.
                ManagedIdentitySourceResult detected =
                    await GetDetectedSourceForTokenRequestAsync(requestContext, isMtlsPopRequested, cancellationToken)
                        .ConfigureAwait(false);

                // 2) Apply per-request fallback policy:
                //    If IMDSv2 is cached but this request is NOT PoP, we serve it via IMDSv1 for this call only.
                ManagedIdentitySource effective =
                    ApplyPerRequestImdsFallbackIfNeeded(requestContext, detected.Source, isMtlsPopRequested);

                // 3) Enforce policy constraints.
                EnforcePreviewSwitchRules(effective, isMtlsPopRequested);
                EnforcePopRequiresImdsV2(effective, isMtlsPopRequested);

                // 4) Instantiate the chosen implementation.
                return CreateManagedIdentityInstance(effective, requestContext, detected);
            }
        }

        /// <summary>
        /// Detect managed identity source based on environment variables and (optionally) IMDS probes.
        ///
        /// This method is used both by:
        ///  - Token selection (typically probe=false)
        ///  - Diagnostic/explicit source checks (probe=true)
        ///
        /// Side-effects:
        ///  - Updates <see cref="s_sourceName"/> in ONE place (see CacheDetectedSource).
        /// </summary>
        internal async Task<ManagedIdentitySourceResult> GetManagedIdentitySourceAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            bool probe,
            CancellationToken cancellationToken)
        {
            // 1) Environment-based detection first (cheap, no network).
            //    These sources are "concrete" and should always win.
            ManagedIdentitySource envSource = GetManagedIdentitySourceNoImds(requestContext.Logger);
            if (envSource != ManagedIdentitySource.None)
            {
                // Env-based sources are definitive; cache them directly.
                CacheDetectedSource(envSource, preserveCachedImdsV2: false);
                return new ManagedIdentitySourceResult(envSource);
            }

            // 2) IMDS detection: choose the appropriate probing strategy.
            //    NOTE: these helpers are intentionally side-effect-free; they only compute a "desired" result.
            ManagedIdentitySourceResult desired =
                probe
                    ? await DetectImds_ProbeBothAsync(requestContext, isMtlsPopRequested, cancellationToken).ConfigureAwait(false)
                    : await DetectImds_NoProbeAsync(requestContext, isMtlsPopRequested, cancellationToken).ConfigureAwait(false);

            // 3) Cache update policy:
            //    - If desired is ImdsV2 or None: always overwrite cache (strong signal)
            //    - If desired is Imds (v1) or DefaultToImds sentinel:
            //        preserve cached ImdsV2 so future PoP calls can still use v2 capability.
            bool preserveImdsV2 =
                desired.Source != ManagedIdentitySource.None &&
                desired.Source != ManagedIdentitySource.ImdsV2;

            ManagedIdentitySource cachedAfterUpdate =
                CacheDetectedSource(desired.Source, preserveCachedImdsV2: preserveImdsV2);

            // 4) Return detection result but with the cached source value, preserving diagnostics.
            //    This matches existing semantics (e.g., do not "lose" cached ImdsV2 once detected).
            return CloneWithSource(desired, cachedAfterUpdate);
        }

        /// <summary>
        /// Detects the managed identity source based on the availability of environment variables.
        /// It does not probe IMDS, but it checks for all other sources.
        ///
        /// This method does NOT cache its result (cheap to evaluate),
        /// but callers can cache if needed (GetManagedIdentitySourceAsync does).
        /// </summary>
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

                logger?.Info("[Managed Identity] App Service detected.");
                return ManagedIdentitySource.AppService;
            }

            if (!string.IsNullOrEmpty(msiSecretMachineLearning) && !string.IsNullOrEmpty(msiEndpoint))
            {
                logger?.Info("[Managed Identity] Machine Learning detected.");
                return ManagedIdentitySource.MachineLearning;
            }

            if (!string.IsNullOrEmpty(msiEndpoint))
            {
                logger?.Info("[Managed Identity] Cloud Shell detected.");
                return ManagedIdentitySource.CloudShell;
            }

            if (ValidateAzureArcEnvironment(identityEndpoint, imdsEndpoint, logger))
            {
                logger?.Info("[Managed Identity] Azure Arc detected.");
                return ManagedIdentitySource.AzureArc;
            }

            return ManagedIdentitySource.None;
        }

        /// <summary>
        /// Returns true if the Azure Arc MI environment is detected either via env vars or file presence.
        /// </summary>
        private static bool ValidateAzureArcEnvironment(string identityEndpoint, string imdsEndpoint, ILoggerAdapter logger)
        {
            logger?.Info(
                "[Managed Identity] Checked for sources: Service Fabric, App Service, Machine Learning, and Cloud Shell. They are not available.");

            // Azure Arc via environment variables
            if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(imdsEndpoint))
            {
                logger?.Verbose(() => "[Managed Identity] Azure Arc managed identity is available through environment variables.");
                return true;
            }

            // Azure Arc via file detection
            if (DesktopOsHelper.IsWindows() && File.Exists(Environment.ExpandEnvironmentVariables(WindowsHimdsFilePath)))
            {
                logger?.Verbose(() => "[Managed Identity] Azure Arc managed identity is available through file detection.");
                return true;
            }

            if (DesktopOsHelper.IsLinux() && File.Exists(LinuxHimdsFilePath))
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
                if (!string.IsNullOrEmpty(sourceResult.ImdsV1FailureReason) ||
                    !string.IsNullOrEmpty(sourceResult.ImdsV2FailureReason))
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
        /// The certificate is intentionally NOT disposed here to avoid invalidating caller-held references.
        /// </summary>
        internal void SetRuntimeMtlsBindingCertificate(X509Certificate2 cert)
        {
            Volatile.Write(ref _runtimeMtlsBindingCertificate, cert);
        }

        // =====================================================================
        // Token selection helpers (cache-aware)
        // =====================================================================

        /// <summary>
        /// For token requests, we avoid probing by default (perf) and rely on:
        ///  - env-based sources (no network)
        ///  - DefaultToImds sentinel for non-PoP requests (means "use IMDSv1 by default")
        ///  - implicit IMDSv2 probe for PoP requests (because PoP requires v2)
        ///
        /// Cache-aware behavior:
        ///  - If cache is None: perform detection
        ///  - If cache is v1-ish and PoP is requested: re-evaluate so v2 can be implicitly probed
        /// </summary>
        private async Task<ManagedIdentitySourceResult> GetDetectedSourceForTokenRequestAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            if (s_sourceName == ManagedIdentitySource.None)
            {
                return await GetManagedIdentitySourceAsync(
                        requestContext,
                        isMtlsPopRequested,
                        probe: false,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            ManagedIdentitySource cached = s_sourceName;

            // If PoP is requested but cache is v1-ish (DefaultToImds/Imds), re-evaluate to allow v2 implicit probe.
            if (isMtlsPopRequested && IsImdsV1OrUnprobed(cached))
            {
                requestContext.Logger.Info(
                    "[Managed Identity] PoP requested; cached source is not ImdsV2. Re-evaluating to allow IMDSv2 probe.");

                return await GetManagedIdentitySourceAsync(
                        requestContext,
                        isMtlsPopRequested: true,
                        probe: false,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return new ManagedIdentitySourceResult(cached);
        }

        /// <summary>
        /// Preview behavior:
        ///  - If IMDSv2 is cached but the request is non-PoP, serve the token via IMDSv1 for THIS request only.
        ///  - Keep cached IMDSv2 so future PoP requests can use it.
        /// </summary>
        private static ManagedIdentitySource ApplyPerRequestImdsFallbackIfNeeded(
            RequestContext requestContext,
            ManagedIdentitySource detectedSource,
            bool isMtlsPopRequested)
        {
            if (detectedSource == ManagedIdentitySource.ImdsV2 && !isMtlsPopRequested)
            {
                requestContext.Logger.Info(
                    "[Managed Identity] ImdsV2 detected, but mTLS PoP was not requested. Falling back to ImdsV1 for this request only. Please use the \"WithMtlsProofOfPossession\" API to request a token via ImdsV2.");

                // Mark that this process has now used IMDSv1 while v2 is cached (preview guard).
                s_imdsV1UsedForPreview = true;

                // IMPORTANT: do NOT overwrite s_sourceName. This is a per-request fallback only.
                return ManagedIdentitySource.Imds;
            }

            return detectedSource;
        }

        /// <summary>
        /// Preview guard:
        ///  - If the process already used IMDSv1 fallback while IMDSv2 is cached,
        ///    disallow switching back to IMDSv2 PoP in the same process.
        /// </summary>
        private static void EnforcePreviewSwitchRules(ManagedIdentitySource effectiveSource, bool isMtlsPopRequested)
        {
            if (effectiveSource == ManagedIdentitySource.ImdsV2 &&
                isMtlsPopRequested &&
                s_imdsV1UsedForPreview)
            {
                throw new MsalClientException(
                    MsalError.CannotSwitchBetweenImdsVersionsForPreview,
                    MsalErrorMessage.CannotSwitchBetweenImdsVersionsForPreview);
            }
        }

        /// <summary>
        /// PoP requires IMDSv2.
        /// If we ended up on a v1-ish state, fail fast (do not silently use IMDSv1).
        /// </summary>
        private static void EnforcePopRequiresImdsV2(ManagedIdentitySource effectiveSource, bool isMtlsPopRequested)
        {
            if (isMtlsPopRequested && IsImdsV1OrUnprobed(effectiveSource))
            {
                throw new MsalClientException(
                    MsalError.MtlsPopTokenNotSupportedinImdsV1,
                    MsalErrorMessage.MtlsPopTokenNotSupportedinImdsV1);
            }
        }

        /// <summary>
        /// Constructs the concrete managed identity source implementation.
        /// DefaultToImds is a sentinel and is normalized to IMDSv1 for instantiation.
        /// </summary>
        private static AbstractManagedIdentity CreateManagedIdentityInstance(
            ManagedIdentitySource effectiveSource,
            RequestContext requestContext,
            ManagedIdentitySourceResult detectedResult)
        {
            if (IsDefaultToImds(effectiveSource))
            {
                effectiveSource = ManagedIdentitySource.Imds;
            }

            return effectiveSource switch
            {
                ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.MachineLearning => MachineLearningManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),

                ManagedIdentitySource.ImdsV2 => ImdsV2ManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.Imds => ImdsManagedIdentitySource.Create(requestContext),

                _ => throw CreateManagedIdentityUnavailableException(detectedResult)
            };
        }

        // =====================================================================
        // IMDS detection helpers
        // =====================================================================

        /// <summary>
        /// NO-PROBE detection strategy:
        ///  - Non-PoP: return DefaultToImds sentinel (no network calls will be made here)
        ///  - PoP: implicitly probe IMDSv2 even when probe==false; DO NOT probe v1
        /// </summary>
        private async Task<ManagedIdentitySourceResult> DetectImds_NoProbeAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            // Non-PoP: no network I/O, return sentinel.
            if (!isMtlsPopRequested)
            {
                requestContext.Logger.Info(
                    "[Managed Identity] No environment-based source detected. Returning DefaultToImds without probing.");

                return new ManagedIdentitySourceResult(DefaultToImdsSentinel);
            }

            // PoP requires v2: implicitly probe v2 even though probe==false.
            requestContext.Logger.Info(
                "[Managed Identity] PoP requested; probing IMDSv2 even though probe == false.");

            var (imdsV2Success, imdsV2Failure) =
                await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(
                        requestContext,
                        ImdsVersion.V2,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (imdsV2Success)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV2 detected.");
                return new ManagedIdentitySourceResult(ManagedIdentitySource.ImdsV2);
            }

            // We intentionally do NOT probe v1 here:
            // - If v1 exists, it cannot satisfy PoP anyway.
            // - Probing v1 adds latency / risk without changing the outcome for PoP.
            requestContext.Logger.Info(
                "[Managed Identity] IMDSv2 probe failed for PoP request. Skipping IMDSv1 probe.");

            return new ManagedIdentitySourceResult(DefaultToImdsSentinel)
            {
                ImdsV2FailureReason = imdsV2Failure
            };
        }

        /// <summary>
        /// Forced probe strategy (probe==true):
        ///  - Probe BOTH endpoints (v2 + v1).
        ///  - Prefer v2 only if caller indicates PoP interest (isMtlsPopRequested==true).
        ///  - Otherwise, return v1 if available (supports v1-only environments like AKS).
        /// </summary>
        private async Task<ManagedIdentitySourceResult> DetectImds_ProbeBothAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            string imdsV2FailureReason = null;
            string imdsV1FailureReason = null;

            var (imdsV2Ok, imdsV2Failure) =
                await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(
                        requestContext,
                        ImdsVersion.V2,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!imdsV2Ok)
            {
                imdsV2FailureReason = imdsV2Failure;
            }

            var (imdsV1Ok, imdsV1Failure) =
                await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(
                        requestContext,
                        ImdsVersion.V1,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (!imdsV1Ok)
            {
                imdsV1FailureReason = imdsV1Failure;
            }

            // Prefer v2 only if PoP interest is indicated and v2 is reachable.
            if (isMtlsPopRequested && imdsV2Ok)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV2 detected.");
                return new ManagedIdentitySourceResult(ManagedIdentitySource.ImdsV2)
                {
                    ImdsV1FailureReason = imdsV1FailureReason
                };
            }

            // Otherwise, fall back to v1 if reachable (AKS/v1-only scenarios).
            if (imdsV1Ok)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV1 detected.");
                return new ManagedIdentitySourceResult(ManagedIdentitySource.Imds)
                {
                    ImdsV2FailureReason = imdsV2FailureReason
                };
            }

            // Neither endpoint reachable.
            requestContext.Logger.Info($"[Managed Identity] {MsalErrorMessage.ManagedIdentityAllSourcesUnavailable}");

            return new ManagedIdentitySourceResult(ManagedIdentitySource.None)
            {
                ImdsV1FailureReason = imdsV1FailureReason,
                ImdsV2FailureReason = imdsV2FailureReason
            };
        }

        // =====================================================================
        // Cache + sentinel helpers (the only place that "knows" DefaultToImds is obsolete)
        // =====================================================================

        /// <summary>
        /// DefaultToImds is a sentinel value used by legacy detection logic:
        /// - "No env-based source detected, and we intentionally did not probe IMDS."
        ///
        /// It is being marked obsolete, but we still use it internally as a sentinel.
        /// This property is the SINGLE place that needs pragma suppression.
        /// </summary>
        private static ManagedIdentitySource DefaultToImdsSentinel
        {
            get
            {
#pragma warning disable CS0618 // DefaultToImds is obsolete (kept temporarily as sentinel)
                return ManagedIdentitySource.DefaultToImds;
#pragma warning restore CS0618
            }
        }

        private static bool IsDefaultToImds(ManagedIdentitySource source) =>
            source == DefaultToImdsSentinel;

        private static bool IsImdsV1OrUnprobed(ManagedIdentitySource source) =>
            source == ManagedIdentitySource.Imds || IsDefaultToImds(source);

        /// <summary>
        /// Updates the process-wide detected source cache.
        ///
        /// Cache overwrite rules:
        /// - If desiredSource is ImdsV2 or None: always overwrite cache (strong signal)
        /// - If desiredSource is Imds or DefaultToImds: preserve cached ImdsV2 when preserveCachedImdsV2==true
        ///
        /// Returns the cached value after applying the update.
        /// </summary>
        private static ManagedIdentitySource CacheDetectedSource(
            ManagedIdentitySource desiredSource,
            bool preserveCachedImdsV2)
        {
            // Strong signals always overwrite.
            if (desiredSource == ManagedIdentitySource.ImdsV2 ||
                desiredSource == ManagedIdentitySource.None)
            {
                s_sourceName = desiredSource;
                return s_sourceName;
            }

            // Preserve IMDSv2 capability once detected (so future PoP calls can still use it),
            // unless the caller explicitly wants to overwrite it (preserveCachedImdsV2==false).
            if (preserveCachedImdsV2 && s_sourceName == ManagedIdentitySource.ImdsV2)
            {
                return s_sourceName;
            }

            s_sourceName = desiredSource;
            return s_sourceName;
        }

        /// <summary>
        /// Returns a new result object if we need to change the source,
        /// while preserving failure reason diagnostics.
        /// </summary>
        private static ManagedIdentitySourceResult CloneWithSource(
            ManagedIdentitySourceResult original,
            ManagedIdentitySource newSource)
        {
            if (original.Source == newSource)
            {
                return original;
            }

            return new ManagedIdentitySourceResult(newSource)
            {
                ImdsV1FailureReason = original.ImdsV1FailureReason,
                ImdsV2FailureReason = original.ImdsV2FailureReason
            };
        }
    }
}
