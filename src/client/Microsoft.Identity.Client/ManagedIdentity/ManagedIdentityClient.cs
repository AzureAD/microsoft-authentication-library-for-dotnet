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
        internal static ManagedIdentitySource s_sourceName = ManagedIdentitySource.None;
        // Preview guard: once we fall back to IMDSv1 while IMDSv2 is cached,
        // disallow switching to IMDSv2 PoP in the same process (preview behavior).
        internal static bool s_imdsV1UsedForPreview = false;

        // Holds the most recently minted mTLS binding certificate for this application instance.
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
            AbstractManagedIdentity msi = await GetOrSelectManagedIdentitySourceAsync(requestContext, parameters.IsMtlsPopRequested, cancellationToken).ConfigureAwait(false);
            return await msi.AuthenticateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        // This method tries to create managed identity source for different sources, if none is created then defaults to IMDS.
        private async Task<AbstractManagedIdentity> GetOrSelectManagedIdentitySourceAsync(
            RequestContext requestContext,
            bool isMtlsPopRequested,
            CancellationToken cancellationToken)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                requestContext.Logger.Info($"[Managed Identity] Selecting managed identity source if not cached. Cached value is {s_sourceName} ");

                ManagedIdentitySource source;

                // If the source is not already set, detect it from environment variables (no IMDS probe).
                if (s_sourceName == ManagedIdentitySource.None)
                {
                    source = GetManagedIdentitySourceNoImds(requestContext.Logger);
                    if (source != ManagedIdentitySource.None)
                    {
                        s_sourceName = source;
                    }
                    else
                    {
                        // No environment-based source detected; use IMDS as the default fallback (no probe).
                        requestContext.Logger.Info("[Managed Identity] No environment-based source detected; defaulting to IMDS.");
#pragma warning disable CS0618 // DefaultToImds is intentionally used as an internal sentinel
                        s_sourceName = ManagedIdentitySource.DefaultToImds;
                        source = ManagedIdentitySource.DefaultToImds;
#pragma warning restore CS0618
                    }
                }
                else
                {
                    source = s_sourceName;
                }

                // When the source is DefaultToImds and mTLS PoP is requested,
                // implicitly probe IMDSv2 to confirm it is available before proceeding.
#pragma warning disable CS0618 // DefaultToImds is intentionally used as an internal sentinel
                if (source == ManagedIdentitySource.DefaultToImds && isMtlsPopRequested)
#pragma warning restore CS0618
                {
                    var (imdsV2Success, imdsV2Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V2, cancellationToken).ConfigureAwait(false);
                    if (imdsV2Success)
                    {
                        requestContext.Logger.Info("[Managed Identity] ImdsV2 detected via implicit probe for mTLS PoP.");
                        s_sourceName = ManagedIdentitySource.ImdsV2;
                        source = ManagedIdentitySource.ImdsV2;
                    }
                    else
                    {
                        requestContext.Logger.Info($"[Managed Identity] IMDSv2 not available for mTLS PoP: {imdsV2Failure}");
                        throw new MsalClientException(
                            MsalError.MtlsPopTokenNotSupportedinImdsV1,
                            MsalErrorMessage.MtlsPopTokenNotSupportedinImdsV1);
                    }
                }

                // If the source has already been set to ImdsV2 (via this method, or GetManagedIdentitySourceAsync in ManagedIdentityApplication.cs)
                // and mTLS PoP was NOT requested: fall back to ImdsV1, because ImdsV2 currently only supports mTLS PoP requests
                if (source == ManagedIdentitySource.ImdsV2 && !isMtlsPopRequested)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 detected, but mTLS PoP was not requested. Falling back to ImdsV1 for this request only. Please use the \"WithMtlsProofOfPossession\" API to request a token via ImdsV2.");
                    
                    // Mark that we used IMDSv1 in this process while IMDSv2 is cached (preview behavior).
                    s_imdsV1UsedForPreview = true;

                    // Do NOT modify s_sourceName; keep cached ImdsV2 so future PoP
                    // requests can leverage it.
                    source = ManagedIdentitySource.Imds;
                }

                // Preview behavior: once we've used IMDSv1 fallback while IMDSv2 is cached,
                // we disallow switching back to ImdsV2 PoP in this process.
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

                // DefaultToImds with no PoP: use IMDSv1 directly (no probe needed)
#pragma warning disable CS0618 // DefaultToImds is intentionally used as an internal sentinel
                if (source == ManagedIdentitySource.DefaultToImds)
#pragma warning restore CS0618
                {
                    source = ManagedIdentitySource.Imds;
                }

                return source switch
                {
                    ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.MachineLearning => MachineLearningManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.ImdsV2 => ImdsV2ManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.Imds => ImdsManagedIdentitySource.Create(requestContext),
                    _ => throw CreateManagedIdentityUnavailableException(null)
                };
            }
        }

        /// <summary>
        /// Detects the managed identity source with optional IMDS probing.
        /// </summary>
        /// <param name="requestContext">The request context for logging and HTTP operations.</param>
        /// <param name="probe">
        /// When <c>false</c> (default): checks environment variables only; returns
        /// <see cref="ManagedIdentitySource.DefaultToImds"/> as a sentinel if no env-based source is found (no IMDS probe).
        /// When <c>true</c>: probes IMDS (v2 first, then v1) if no env-based source is found.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        internal async Task<ManagedIdentitySourceResult> GetManagedIdentitySourceAsync(
            RequestContext requestContext,
            bool probe,
            CancellationToken cancellationToken)
        {
            // First check env vars to avoid the probe if possible
            ManagedIdentitySource source = GetManagedIdentitySourceNoImds(requestContext.Logger);
            if (source != ManagedIdentitySource.None)
            {
                s_sourceName = source;
                return new ManagedIdentitySourceResult(source);
            }

            if (!probe)
            {
                // No probe requested: return DefaultToImds as sentinel meaning "use IMDS as default fallback"
                requestContext.Logger.Info("[Managed Identity] No probe requested; defaulting to IMDS without probing.");
#pragma warning disable CS0618 // DefaultToImds is intentionally used as an internal sentinel
                s_sourceName = ManagedIdentitySource.DefaultToImds;
                return new ManagedIdentitySourceResult(ManagedIdentitySource.DefaultToImds);
#pragma warning restore CS0618
            }

            string imdsV2FailureReason = null;
            string imdsV1FailureReason = null;

            // Probe IMDSv2 first
            var (imdsV2Success, imdsV2Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V2, cancellationToken).ConfigureAwait(false);
            if (imdsV2Success)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV2 detected.");
                s_sourceName = ManagedIdentitySource.ImdsV2;
                return new ManagedIdentitySourceResult(s_sourceName);
            }
            imdsV2FailureReason = imdsV2Failure;

            // Probe IMDSv1 as fallback
            var (imdsV1Success, imdsV1Failure) = await ImdsManagedIdentitySource.ProbeImdsEndpointAsync(requestContext, ImdsVersion.V1, cancellationToken).ConfigureAwait(false);
            if (imdsV1Success)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV1 detected.");
                s_sourceName = ManagedIdentitySource.Imds;
                return new ManagedIdentitySourceResult(s_sourceName);
            }
            imdsV1FailureReason = imdsV1Failure;

            requestContext.Logger.Info($"[Managed Identity] {MsalErrorMessage.ManagedIdentityAllSourcesUnavailable}");
            s_sourceName = ManagedIdentitySource.None;
            
            return new ManagedIdentitySourceResult(s_sourceName)
            {
                ImdsV1FailureReason = imdsV1FailureReason,
                ImdsV2FailureReason = imdsV2FailureReason
            };
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
