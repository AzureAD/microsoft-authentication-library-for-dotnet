// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity.V2;
using System.Security.Cryptography.X509Certificates;

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

        // Holds the most recently minted mTLS binding certificate for this application instance.
        private X509Certificate2 _runtimeMtlsBindingCertificate;
        internal X509Certificate2 RuntimeMtlsBindingCertificate => Volatile.Read(ref _runtimeMtlsBindingCertificate);

        internal static void ResetSourceForTest()
        {
            s_sourceName = ManagedIdentitySource.None;
        }

        internal async Task<ManagedIdentityResponse> SendTokenRequestForManagedIdentityAsync(
            RequestContext requestContext,
            AcquireTokenForManagedIdentityParameters parameters,
            CancellationToken cancellationToken)
        {
            AbstractManagedIdentity msi = await GetOrSelectManagedIdentitySourceAsync(requestContext, parameters.IsMtlsPopRequested).ConfigureAwait(false);
            return await msi.AuthenticateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        // This method tries to create managed identity source for different sources, if none is created then defaults to IMDS.
        private async Task<AbstractManagedIdentity> GetOrSelectManagedIdentitySourceAsync(RequestContext requestContext, bool isMtlsPopRequested)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                requestContext.Logger.Info($"[Managed Identity] Selecting managed identity source if not cached. Cached value is {s_sourceName} ");

                var source = ManagedIdentitySource.None;

                // If the source is not already set, determine it
                if (s_sourceName == ManagedIdentitySource.None)
                {
                    source = await GetManagedIdentitySourceAsync(requestContext).ConfigureAwait(false);
                }
                // Otherwise, check if the source has already been set to ImdsV2 (via this method, or GetManagedIdentitySourceAsync in ManagedIdentityApplication.cs) and mTLS PoP was NOT requested
                // In this case, we need to fall back to ImdsV1, because ImdsV2 currently only supports mTLS PoP requests
                else if ((s_sourceName == ManagedIdentitySource.ImdsV2) && !isMtlsPopRequested)
                {
                    requestContext.Logger.Info("[Managed Identity] ImdsV2 detected, but mTLS PoP was not requested. Falling back to ImdsV1 for this request only. Please use the \"WithMtlsProofOfPossession\" API to request a token via ImdsV2.");

                    // keep the cached source (s_sourceName) as ImdsV2, since the developer may decide to use mTLS PoP in subsequent requests

                    source = ManagedIdentitySource.DefaultToImds;
                }
                else
                {
                    source = s_sourceName;
                }

                return source switch
                {
                    ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.MachineLearning => MachineLearningManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),
                    ManagedIdentitySource.ImdsV2 => ImdsV2ManagedIdentitySource.Create(requestContext),
                    _ => new ImdsManagedIdentitySource(requestContext)
                };
            }
        }

        // Detect managed identity source based on the availability of environment variables and csr metadata probe request.
        // This method is perf sensitive any changes should be benchmarked.
        internal async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync(RequestContext requestContext)
        {
            // First check env vars to avoid the probe if possible
            ManagedIdentitySource source = GetManagedIdentitySourceNoImdsV2(requestContext.Logger);

            // If a source is detected via env vars, use it
            if (source != ManagedIdentitySource.DefaultToImds)
            {
                s_sourceName = source;
                return source;
            }

            // Otherwise, probe IMDSv2
            var response = await ImdsV2ManagedIdentitySource.GetCsrMetadataAsync(requestContext, probeMode: true).ConfigureAwait(false);
            if (response != null)
            {
                requestContext.Logger.Info("[Managed Identity] ImdsV2 detected.");
                s_sourceName = ManagedIdentitySource.ImdsV2;
                return s_sourceName;
            }

            requestContext.Logger.Info("[Managed Identity] IMDSv2 probe failed. Defaulting to IMDSv1.");
            s_sourceName = ManagedIdentitySource.DefaultToImds;
            return s_sourceName;
        }

        // Detect managed identity source based on the availability of environment variables.
        // The result of this method is not cached because reading environment variables is cheap. 
        // This method is perf sensitive any changes should be benchmarked.
        internal static ManagedIdentitySource GetManagedIdentitySourceNoImdsV2(ILoggerAdapter logger = null)
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
                return ManagedIdentitySource.DefaultToImds;
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
        /// Sets (or replaces) the in-memory binding certificate used to prime the mtls_pop scheme on subsequent requests.
        /// The certificate is intentionally NOT disposed here to avoid invalidating caller-held references (e.g., via AuthenticationResult).
        /// </summary>
        /// <remarks>
        /// Lifetime considerations:
        /// - The binding certificate is ephemeral and valid for the token’s binding duration.
        /// - If rotation occurs, older certificates will be eligible for GC once no longer referenced.
        /// - Explicit disposal can be revisited if a deterministic rotation / shutdown strategy is introduced.
        /// </remarks>
        internal void SetRuntimeMtlsBindingCertificate(X509Certificate2 cert)
        {
            Volatile.Write(ref _runtimeMtlsBindingCertificate, cert);
        }
    }
}
