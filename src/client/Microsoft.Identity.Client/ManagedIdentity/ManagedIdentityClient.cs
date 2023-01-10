// Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.Identity.Client.ManagedIdentity
{
    /// <summary>
    /// Class to initialize a managed identity and identify the service.
    /// Original source of code: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/src/ManagedIdentityClient.cs
    /// </summary>
    internal class ManagedIdentityClient
    {
        private readonly ManagedIdentitySource _identitySource;

        public ManagedIdentityClient(RequestContext requestContext)
        {
            using (requestContext.Logger.LogMethodDuration())
            {
                _identitySource = SelectManagedIdentitySource(requestContext);
            }
        }

        internal async Task<AppTokenProviderResult> AppTokenProviderImplAsync(AppTokenProviderParameters parameters)
        {
            ManagedIdentityResponse response = await _identitySource.AuthenticateAsync(parameters, parameters.CancellationToken).ConfigureAwait(false);

            return new AppTokenProviderResult() { AccessToken = response.AccessToken, ExpiresInSeconds = DateTimeHelpers.GetDurationFromNowInSeconds(response.ExpiresOn) };
        }

        // This method tries to create managed identity source for different sources, if none is created then defaults to IMDS.
        private static ManagedIdentitySource SelectManagedIdentitySource(RequestContext requestContext)
        {
            return 
                ServiceFabricManagedIdentitySource.TryCreate(requestContext) ??
                AppServiceManagedIdentitySource.TryCreate(requestContext) ?? 
                CloudShellManagedIdentitySource.TryCreate(requestContext) ??
                AzureArcManagedIdentitySource.TryCreate(requestContext) ??
                new ImdsManagedIdentitySource(requestContext);
        }
    }
}
