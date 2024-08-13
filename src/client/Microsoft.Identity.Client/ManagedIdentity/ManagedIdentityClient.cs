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

        public ManagedIdentityClient(RequestContext requestContext)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                _identitySource = SelectManagedIdentitySource(requestContext);
            }
        }

        internal Task<ManagedIdentityResponse> SendTokenRequestForManagedIdentityAsync(AcquireTokenForManagedIdentityParameters parameters, CancellationToken cancellationToken)
        {
            return _identitySource.AuthenticateAsync(parameters, cancellationToken);
        }

        // This method tries to create managed identity source for different sources, if none is created then defaults to IMDS.
        private static AbstractManagedIdentity SelectManagedIdentitySource(RequestContext requestContext)
        {
            return GetManagedIdentitySource(requestContext.Logger) switch
            {
                ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.Create(requestContext),
                ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.Create(requestContext),
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
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;
            string podIdentityEndpoint = EnvironmentVariables.PodIdentityEndpoint;

            
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
