﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Extensibility;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.ApiConfig.Parameters;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Class to initialize a managed identity and identify the service.
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/ManagedIdentityClient.cs
    /// </summary>
    internal class ManagedIdentityClient
    {
        private readonly AbstractManagedIdentity _identitySource;
        internal static Lazy<ManagedIdentitySource> s_managedIdentitySourceDetected = new Lazy<ManagedIdentitySource>(() => GetManagedIdentitySource());

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
            return s_managedIdentitySourceDetected.Value switch
            {
                ManagedIdentitySource.ServiceFabric => ServiceFabricManagedIdentitySource.TryCreate(requestContext),
                ManagedIdentitySource.AppService => AppServiceManagedIdentitySource.TryCreate(requestContext),
                ManagedIdentitySource.CloudShell => CloudShellManagedIdentitySource.TryCreate(requestContext),
                ManagedIdentitySource.AzureArc => AzureArcManagedIdentitySource.TryCreate(requestContext),
                _ => new ImdsManagedIdentitySource(requestContext)
            };
        }

        // Detect managed identity source based on the availability of environment variables.
        private static ManagedIdentitySource GetManagedIdentitySource()
        {
            string identityEndpoint = EnvironmentVariables.IdentityEndpoint;
            string identityHeader = EnvironmentVariables.IdentityHeader;
            string identityServerThumbprint = EnvironmentVariables.IdentityServerThumbprint;
            string msiSecret = EnvironmentVariables.IdentityHeader;
            string msiEndpoint = EnvironmentVariables.MsiEndpoint;
            string imdsEndpoint = EnvironmentVariables.ImdsEndpoint;
            string podIdentityEndpoint = EnvironmentVariables.PodIdentityEndpoint;

            if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(identityHeader) && 
                !string.IsNullOrEmpty(identityServerThumbprint))
            {
                return ManagedIdentitySource.ServiceFabric;
            }
            else if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(identityHeader))
            {
                return ManagedIdentitySource.AppService;
            }
            else if (!string.IsNullOrEmpty(msiEndpoint))
            {
                return ManagedIdentitySource.CloudShell;
            }
            else if (!string.IsNullOrEmpty(identityEndpoint) && !string.IsNullOrEmpty(imdsEndpoint))
            {
                return ManagedIdentitySource.AzureArc;
            }
            else if (!string.IsNullOrEmpty(podIdentityEndpoint))
            {
                return ManagedIdentitySource.Imds;
            }
            else
            {
                return ManagedIdentitySource.Imds;
            }
        }
    }
}
