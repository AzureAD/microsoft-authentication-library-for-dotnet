// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    /// <summary>
    /// Legacy (non-credential-based) MI flow using ManagedIdentityClient.SendTokenRequestForManagedIdentityAsync.
    /// </summary>
    internal sealed class LegacyManagedIdentityAuthRequest : ManagedIdentityAuthRequestBase
    {
        public LegacyManagedIdentityAuthRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenForManagedIdentityParameters managedIdentityParameters)
            : base(serviceBundle, authenticationRequestParameters, managedIdentityParameters)
        {
        }

        protected override async Task<AuthenticationResult> SendTokenRequestAsync(
            ILoggerAdapter logger,
            CancellationToken cancellationToken)
        {
            logger.Info("[ManagedIdentityRequest:Legacy] Acquiring a token from the managed identity endpoint.");

            ManagedIdentityClient managedIdentityClient =
                await ManagedIdentityClient.CreateAsync(
                    AuthenticationRequestParameters.RequestContext,
                    cancellationToken).ConfigureAwait(false);

            ManagedIdentityResponse managedIdentityResponse =
                await managedIdentityClient
                    .SendTokenRequestForManagedIdentityAsync(_managedIdentityParameters, cancellationToken)
                    .ConfigureAwait(false);

            var msalTokenResponse = MsalTokenResponse.CreateFromManagedIdentityResponse(managedIdentityResponse);
            msalTokenResponse.Scope = AuthenticationRequestParameters.Scope.AsSingleString();

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse)
                .ConfigureAwait(false);
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> _) => null;
    }
}
