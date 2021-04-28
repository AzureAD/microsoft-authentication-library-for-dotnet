// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ByRefreshTokenRequest : RequestBase
    {
        private readonly AcquireTokenByRefreshTokenParameters _refreshTokenParameters;

        public ByRefreshTokenRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByRefreshTokenParameters refreshTokenParameters)
            : base(serviceBundle, authenticationRequestParameters, refreshTokenParameters)
        {
            _refreshTokenParameters = refreshTokenParameters;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            AuthenticationRequestParameters.RequestContext.Logger.Verbose(LogMessages.BeginningAcquireByRefreshToken);
            await ResolveAuthorityAsync().ConfigureAwait(false);
            var msalTokenResponse = await SendTokenRequestAsync(
                                        GetBodyParameters(_refreshTokenParameters.RefreshToken),
                                        cancellationToken).ConfigureAwait(false);

            if (msalTokenResponse.RefreshToken == null)
            {
                AuthenticationRequestParameters.RequestContext.Logger.Info(MsalErrorMessage.NoRefreshTokenInResponse);
                throw new MsalServiceException(msalTokenResponse.Error, msalTokenResponse.ErrorDescription, null);
            }

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        private Dictionary<string, string> GetBodyParameters(string refreshTokenSecret)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.RefreshToken,
                [OAuth2Parameter.RefreshToken] = refreshTokenSecret
            };

            return dict;
        }
    }
}
