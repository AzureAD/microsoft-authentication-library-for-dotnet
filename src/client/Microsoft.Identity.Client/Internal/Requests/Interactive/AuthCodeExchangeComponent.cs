// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
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
        private readonly string _clientInfo;

        public AuthCodeExchangeComponent(
            AuthenticationRequestParameters requestParams,
            AcquireTokenInteractiveParameters interactiveParameters,
            string authorizationCode,
            string pkceCodeVerifier,
            string clientInfo)
        {
            _requestParams = requestParams ?? throw new ArgumentNullException(nameof(requestParams));
            _interactiveParameters = interactiveParameters ?? throw new ArgumentNullException(nameof(interactiveParameters));
            _authorizationCode = authorizationCode ?? throw new ArgumentNullException(nameof(authorizationCode));
            _pkceCodeVerifier = pkceCodeVerifier ?? throw new ArgumentNullException(nameof(pkceCodeVerifier));
            _clientInfo = clientInfo;

            _tokenClient = new TokenClient(requestParams);
            _interactiveParameters.LogParameters(requestParams.RequestContext.Logger);
        }

        public Task<MsalTokenResponse> FetchTokensAsync(CancellationToken cancellationToken)
        {
            AddCcsHeadersToTokenClient();
            return _tokenClient.SendTokenRequestAsync(GetBodyParameters(), cancellationToken: cancellationToken);
        }

        private Dictionary<string, string> GetBodyParameters()
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.ClientInfo] = "1",
                [OAuth2Parameter.GrantType] = OAuth2GrantType.AuthorizationCode,
                [OAuth2Parameter.Code] = _authorizationCode,
                [OAuth2Parameter.RedirectUri] = _requestParams.RedirectUri.OriginalString,
                [OAuth2Parameter.PkceCodeVerifier] = _pkceCodeVerifier
            };

            return dict;
        }

        private void AddCcsHeadersToTokenClient()
        {
            if (!string.IsNullOrEmpty(_clientInfo))
            {
                var clientInfo = ClientInfo.CreateFromJson(_clientInfo);

                _tokenClient.AddHeaderToClient(Constants.CcsRoutingHintHeader,
                                               CoreHelpers.GetCcsClientInfoHint(clientInfo.UniqueObjectIdentifier,
                                                                                  clientInfo.UniqueTenantIdentifier));
            }
            else if (!string.IsNullOrEmpty(_interactiveParameters.LoginHint))
            {
                _tokenClient.AddHeaderToClient(Constants.CcsRoutingHintHeader, CoreHelpers.GetCcsUpnHint(_interactiveParameters.LoginHint));
            }
        }
    }
}
