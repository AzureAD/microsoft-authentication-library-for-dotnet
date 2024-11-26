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
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Class to initialize a managed identity and identify the service.
    /// </summary>
    internal class ManagedIdentityClient
    {
        private const string WindowsHimdsFilePath = "%Programfiles%\\AzureConnectedMachineAgent\\himds.exe";
        private const string LinuxHimdsFilePath = "/opt/azcmagent/bin/himds";

        // Cache for the managed identity source
        private static ManagedIdentitySource? s_cachedManagedIdentitySource;
        private static readonly object s_lock = new();
        private readonly AbstractManagedIdentity _identitySource;

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
            lock (s_lock)
            {
                s_cachedManagedIdentitySource = null;
            }
        }

        internal Task<ManagedIdentityResponse> SendTokenRequestForManagedIdentityAsync(AcquireTokenForManagedIdentityParameters parameters, CancellationToken cancellationToken)
        {
            return _identitySource.AuthenticateAsync(parameters, cancellationToken);
        }

        // This method tries to create managed identity source for different sources, if none is created then defaults to IMDS.
        private static async Task<AbstractManagedIdentity> SelectManagedIdentitySourceAsync(RequestContext requestContext, CancellationToken cancellationToken = default)
        {
            ManagedIdentitySource source = await GetOrCreateManagedIdentitySourceAsync(requestContext, cancellationToken).ConfigureAwait(false);

            return source switch
            {
                ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),
                _ => new ImdsManagedIdentitySource(requestContext)
            };
        }

        // Caches the result of detecting the managed identity source.
        internal static async Task<ManagedIdentitySource> GetOrCreateManagedIdentitySourceAsync(RequestContext requestContext, CancellationToken cancellationToken = default)
        {
            requestContext.ServiceBundle.ApplicationLogger?.Verbose(() => s_cachedManagedIdentitySource.HasValue
                ? "[Managed Identity] Using cached managed identity source."
                : "[Managed Identity] Computing managed identity source asynchronously.");

            if (s_cachedManagedIdentitySource.HasValue)
            {
                return s_cachedManagedIdentitySource.Value;
            }

            lock (s_lock)
            {
                if (s_cachedManagedIdentitySource.HasValue)
                {
                    return s_cachedManagedIdentitySource.Value;
                }
            }

            // Call the new async GetManagedIdentitySourceAsync method
            var managedIdentitySource = await GetManagedIdentitySourceAsync(
                requestContext.ServiceBundle,
                cancellationToken).ConfigureAwait(false);

            lock (s_lock)
            {
                if (!s_cachedManagedIdentitySource.HasValue)
                {
                    s_cachedManagedIdentitySource = managedIdentitySource;
                }
            }

            return s_cachedManagedIdentitySource.Value;
        }

        // Detect managed identity source based on the availability of environment variables
        // or the new /credential endpoint. And cache the result of this method.
        internal static ManagedIdentitySource GetManagedIdentitySource(ILoggerAdapter logger = null)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string identityHeader = EnvironmentVariables.IdentityHeader;
            string identityServerThumbprint = EnvironmentVariables.IdentityServerThumbprint;
            string msiEndpoint = EnvironmentVariables.MsiEndpoint;
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;

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
                return ManagedIdentitySource.DefaultToImds;
            }
        }

        public static async Task<ManagedIdentitySource> GetManagedIdentitySourceAsync(
            IServiceBundle serviceBundle, 
            CancellationToken cancellationToken = default)
        {
            if (serviceBundle == null)
            {
                throw new ArgumentNullException(nameof(serviceBundle), "ServiceBundle is required to initialize the probe manager.");
            }

            serviceBundle.ApplicationLogger.Verbose(() => s_cachedManagedIdentitySource.HasValue
                ? "[Managed Identity] Using cached managed identity source."
                : "[Managed Identity] Computing managed identity source asynchronously.");

            if (s_cachedManagedIdentitySource.HasValue)
            {
                return s_cachedManagedIdentitySource.Value;
            }

            lock (s_lock)
            {
                if (s_cachedManagedIdentitySource.HasValue)
                {
                    return s_cachedManagedIdentitySource.Value;
                }
            }

            // Initialize the probe manager if not already initialized
            var probeManager = new ImdsCredentialProbeManager(
                    serviceBundle.HttpManager,
                    serviceBundle.ApplicationLogger);

            ManagedIdentitySource result = await ComputeManagedIdentitySourceAsync(probeManager, serviceBundle.ApplicationLogger, cancellationToken).ConfigureAwait(false);

            lock (s_lock)
            {
                if (!s_cachedManagedIdentitySource.HasValue)
                {
                    s_cachedManagedIdentitySource = result;
                }
            }

            return s_cachedManagedIdentitySource.Value;
        }

        private static async Task<ManagedIdentitySource> ComputeManagedIdentitySourceAsync(
            ImdsCredentialProbeManager imdsCredentialProbeManager,
            ILoggerAdapter logger, 
            CancellationToken cancellationToken)
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string identityHeader = EnvironmentVariables.IdentityHeader;
            string identityServerThumbprint = EnvironmentVariables.IdentityServerThumbprint;
            string msiEndpoint = EnvironmentVariables.MsiEndpoint;
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;

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
                bool isSuccess = await imdsCredentialProbeManager.ExecuteProbeAsync(cancellationToken).ConfigureAwait(false);

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
            else
            {
                logger?.Warning("[Managed Identity] Azure Arc managed identity cannot be configured on a platform other than Windows and Linux.");
            }

            logger?.Verbose(() => "[Managed Identity] Azure Arc managed identity is not available.");
            return false;
        }
    }
}
