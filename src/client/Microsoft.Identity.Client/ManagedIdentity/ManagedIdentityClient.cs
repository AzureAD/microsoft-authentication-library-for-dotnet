// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using System.IO;
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
            AbstractManagedIdentity msi = await GetOrSelectManagedIdentitySourceAsync(requestContext).ConfigureAwait(false);
            return await msi.AuthenticateAsync(parameters, cancellationToken).ConfigureAwait(false);
        }

        // This method tries to create managed identity source for different sources, if none is created then defaults to IMDS.
        private async Task<AbstractManagedIdentity> GetOrSelectManagedIdentitySourceAsync(RequestContext requestContext)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                requestContext.Logger.Info($"[Managed Identity] Selecting managed identity source if not cached. Cached value is {s_sourceName} ");

                var source = (s_sourceName != ManagedIdentitySource.None) ? s_sourceName : await GetManagedIdentitySourceAsync(requestContext).ConfigureAwait(false);
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
            ManagedIdentitySource source = GetManagedIdentitySourceNoImdsV2(requestContext.Logger);

            if (source != ManagedIdentitySource.DefaultToImds)
            {
                return source;
            }

            // probe IMDSv2
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
        /// Sets the in-memory binding certificate used to prime the mtls_pop scheme on subsequent requests.
        /// </summary>
        /// <remarks>
        /// Disposing an <see cref="X509Certificate2"/> releases resources for this in-memory instance;
        /// it does <b>not</b> remove a certificate from any OS certificate store (store removal requires <see cref="X509Store.Remove(X509Certificate2)"/>).
        /// </remarks>
        internal void SetRuntimeMtlsBindingCertificate(X509Certificate2 cert)
        {
            // Atomically swap the reference and dispose the previous one (if different).
            var old = System.Threading.Interlocked.Exchange(ref _runtimeMtlsBindingCertificate, cert);

            // If the same instance is passed again, do not dispose it (it is now the current value).
            if (!object.ReferenceEquals(old, cert))
            {
                old?.Dispose();
            }
        }
    }
}
