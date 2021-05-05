// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class AuthCodeExchangeComponent : ITokenRequestComponent
    {
        private readonly AuthenticationRequestParameters _requestParams;
        private readonly AcquireTokenInteractiveParameters _interactiveParameters;
        private readonly string _authorizationCode;
        private readonly string _pkceCodeVerifier;
        private readonly TokenClient _tokenClient;

        public AuthCodeExchangeComponent( 
            AuthenticationRequestParameters requestParams,
            AcquireTokenInteractiveParameters interactiveParameters,
            string authorizationCode,
            string pkceCodeVerifier)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _interactiveParameters = interactiveParameters ?? throw new ArgumentNullException(nameof(interactiveParameters));
            _authorizationCode = authorizationCode ?? throw new ArgumentNullException(nameof(authorizationCode));
            _pkceCodeVerifier = pkceCodeVerifier ?? throw new ArgumentNullException(nameof(pkceCodeVerifier));

            _tokenClient = new TokenClient(requestParams);
            _interactiveParameters.LogParameters(requestParams.RequestContext.Logger);
        }

        public Task<MsalTokenResponse> FetchTokensAsync(CancellationToken cancellationToken)
        {
            _tokenClient.AddHeaderToClient(Constants.OidCCSHeader, CoreHelpers.GetCCSUpnHeader(_interactiveParameters.LoginHint));
            return _tokenClient.SendTokenRequestAsync(GetBodyParameters());
        }

        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.AuthorizationCode,
                [OAuth2Parameter.Code] = _authorizationCode,
                [OAuth2Parameter.RedirectUri] = _requestParams.RedirectUri.OriginalString,
                [OAuth2Parameter.PkceCodeVerifier] = _pkceCodeVerifier
            };

            return dict;
        }
    }
}
