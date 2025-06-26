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

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Class to initialize a managed identity and identify the service.
    /// </summary>
    internal class ManagedIdentityClient
    {
        private const string WindowsHimdsFilePath = "%Programfiles%\\AzureConnectedMachineAgent\\himds.exe";
        private const string LinuxHimdsFilePath = "/opt/azcmagent/bin/himds";
        private readonly AbstractManagedIdentity _identitySource;
        // Cache for the managed identity source
        private static ManagedIdentitySource? s_cachedManagedIdentitySource;
        private static readonly SemaphoreSlim s_credentialSemaphore = new(1, 1);

        internal static async Task<ManagedIdentityClient> CreateAsync(RequestContext requestContext, CancellationToken cancellationToken = default)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext), "RequestContext cannot be null.");
            }

            requestContext.Logger?.Info("[ManagedIdentityClient] Creating ManagedIdentityClient.");

            AbstractManagedIdentity identitySource = await SelectManagedIdentitySourceAsync(requestContext, cancellationToken).ConfigureAwait(false);

            requestContext.Logger?.Info($"[ManagedIdentityClient] Managed identity source selected: {identitySource.GetType().Name}.");

            return new ManagedIdentityClient(identitySource);
        }

        private ManagedIdentityClient(AbstractManagedIdentity identitySource)
        {
            _identitySource = identitySource ?? throw new ArgumentNullException(nameof(identitySource), "Identity source cannot be null.");
        }

        /// <summary>
        /// Resets the cached managed identity source. Used only for testing purposes.
        /// </summary>
        internal static void ResetManagedIdentitySourceCache()
        {
            s_cachedManagedIdentitySource = null;
        }

        internal Task<ManagedIdentityResponse> SendTokenRequestForManagedIdentityAsync(AcquireTokenForManagedIdentityParameters parameters, CancellationToken cancellationToken)
        {
            return _identitySource.AuthenticateAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// This method tries to create managed identity source for different sources. 
        /// If none is created then defaults to IMDS.
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<AbstractManagedIdentity> SelectManagedIdentitySourceAsync(RequestContext requestContext, CancellationToken cancellationToken = default)
        {
            ManagedIdentitySource source = await GetManagedIdentitySourceAsync(requestContext, cancellationToken).ConfigureAwait(false);

            return source switch
            {
                ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.MachineLearning => MachineLearningManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.Credential => CredentialManagedIdentitySource.Create(requestContext),
                _ => new ImdsManagedIdentitySource(requestContext)
            };
        }

        // Detect managed identity source based on the availability of environment variables.
        // The result of this method is not cached because reading environment variables is cheap. 
        // This method is perf sensitive any changes should be benchmarked.
        internal static ManagedIdentitySource GetManagedIdentitySource(ILoggerAdapter logger = null)
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

        /// <summary>
        /// Compute the managed identity source based on the environment variables and the probe.
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync(
            RequestContext requestContext,
            CancellationToken cancellationToken = default)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext), "RequestContext is required to initialize the probe manager.");
            }

            ILoggerAdapter logger = requestContext.ServiceBundle.ApplicationLogger;

            logger.Verbose(() => s_cachedManagedIdentitySource.HasValue
                ? "[Managed Identity] Using cached managed identity source."
                : "[Managed Identity] Computing managed identity source asynchronously.");

            if (s_cachedManagedIdentitySource.HasValue)
            {
                return s_cachedManagedIdentitySource.Value;
            }

            // Use SemaphoreSlim to prevent multiple threads from computing at the same time
            logger.Verbose(() => "[Managed Identity] Entering managed identity source semaphore.");
            await s_credentialSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[Managed Identity] Entered managed identity source semaphore.");

            try
            {
                // Ensure another thread didn't set this while waiting on semaphore
                if (s_cachedManagedIdentitySource.HasValue)
                {
                    return s_cachedManagedIdentitySource.Value;
                }

                // Initialize probe manager
                var probeManager = new CredentialProbeManager(
                    requestContext);

                // Compute the managed identity source
                s_cachedManagedIdentitySource = await ComputeManagedIdentitySourceAsync(
                    probeManager,
                    requestContext.ServiceBundle.ApplicationLogger,
                    cancellationToken).ConfigureAwait(false);

                logger.Info($"[Managed Identity] Managed identity source determined: {s_cachedManagedIdentitySource.Value}.");

                return s_cachedManagedIdentitySource.Value;
            }
            finally
            {
                s_credentialSemaphore.Release();
                logger.Verbose(() => "[Managed Identity] Released managed identity source semaphore.");
            }
        }

        /// <summary>
        /// Compute the managed identity source based on the environment variables and the probe.
        /// </summary>
        /// <param name="credentialProbeManager"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<ManagedIdentitySource> ComputeManagedIdentitySourceAsync(
            CredentialProbeManager credentialProbeManager,
            ILoggerAdapter logger,
            CancellationToken cancellationToken)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string identityHeader = EnvironmentVariables.IdentityHeader;
            string identityServerThumbprint = EnvironmentVariables.IdentityServerThumbprint;
            string msiEndpoint = EnvironmentVariables.MsiEndpoint;
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;
            string msiSecretMachineLearning = EnvironmentVariables.MsiSecret;

            if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(identityHeader))
            {
                if (!string.IsNullOrEmpty(identityServerThumbprint))
                {
                    return ManagedIdentitySource.ServiceFabric;
                }
                else
                {
                    return ManagedIdentitySource.AppService;
                }
            }
            else if (!string.IsNullOrEmpty(msiSecretMachineLearning) && !string.IsNullOrEmpty(msiEndpoint))
            {
                return ManagedIdentitySource.MachineLearning;
            }
            else if (!string.IsNullOrEmpty(msiEndpoint))
            {
                return ManagedIdentitySource.CloudShell;
            }
            else if (ValidateAzureArcEnvironment(identityEndpoint, imdsEndpoint, logger))
            {
                return ManagedIdentitySource.AzureArc;
            }
            else
            {
                logger?.Info("[Managed Identity] Probing for credential endpoint.");
                bool isSuccess = await credentialProbeManager.ExecuteProbeAsync(cancellationToken).ConfigureAwait(false);

                if (isSuccess)
                {
                    logger?.Info("[Managed Identity] Credential endpoint detected.");
                    return ManagedIdentitySource.Credential;
                }
                else
                {
                    logger?.Verbose(() => "[Managed Identity] Defaulting to IMDS as credential endpoint not detected.");
                    return ManagedIdentitySource.DefaultToImds;
                }
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
    }
}
