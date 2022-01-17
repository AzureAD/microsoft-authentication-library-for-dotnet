// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class ConfidentialAuthCodeRequest : RequestBase
    {
        private readonly AcquireTokenByAuthorizationCodeParameters _authorizationCodeParameters;

        public ConfidentialAuthCodeRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByAuthorizationCodeParameters authorizationCodeParameters)
            : base(serviceBundle, authenticationRequestParameters, authorizationCodeParameters)
        {
            _authorizationCodeParameters = authorizationCodeParameters;
            RedirectUriHelper.Validate(authenticationRequestParameters.RedirectUri);
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);
            var msalTokenResponse = await SendTokenRequestAsync(GetBodyParameters(), cancellationToken).ConfigureAwait(false);
            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse).ConfigureAwait(false);
        }

        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.ClientInfo] = "1",
                [OAuth2Parameter.GrantType] = OAuth2GrantType.AuthorizationCode,
                [OAuth2Parameter.Code] = _authorizationCodeParameters.AuthorizationCode,
                [OAuth2Parameter.RedirectUri] = AuthenticationRequestParameters.RedirectUri.OriginalString
            };

            if (!string.IsNullOrEmpty(_authorizationCodeParameters.PkceCodeVerifier))
            {
                dict[OAuth2Parameter.PkceCodeVerifier] = _authorizationCodeParameters.PkceCodeVerifier;
            }

            if (_authorizationCodeParameters.SpaCode)
            {
                dict[OAuth2Parameter.SpaCode] = Constants.EnableSpaAuthCode;
            }

            return dict;
        }
    }
}
