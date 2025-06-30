// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class LegacyManagedIdentityAuthRequest : ManagedIdentityAuthRequest
    {
        public LegacyManagedIdentityAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
        }

        protected override async Task<AuthenticationResult> SendTokenRequestForManagedIdentityAsync(
            ILoggerAdapter logger,
            CancellationToken cancellationToken)
        {
            logger.Info("[ManagedIdentityRequest] Acquiring a token from the managed identity endpoint.");

            await ResolveAuthorityAsync().ConfigureAwait(false);

            ManagedIdentityClient managedIdentityClient =
                await ManagedIdentityClient.CreateAsync(
                        AuthenticationRequestParameters.RequestContext,
                        cancellationToken)
                    .ConfigureAwait(false);

            ManagedIdentityResponse managedIdentityResponse =
                await managedIdentityClient
                    .SendTokenRequestForManagedIdentityAsync(_managedIdentityParameters, cancellationToken)
                    .ConfigureAwait(false);

            var msalTokenResponse = MsalTokenResponse.CreateFromManagedIdentityResponse(managedIdentityResponse);
            msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }
    }
}
