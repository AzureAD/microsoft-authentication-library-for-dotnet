// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class UserFederatedIdentityCredentialRequest : RequestBase
    {
        private readonly AcquireTokenByUserFederatedIdentityCredentialParameters _userFicParameters;

        public UserFederatedIdentityCredentialRequest(
            IServiceBundle serviceBundle,
            AuthenticationRequestParameters authenticationRequestParameters,
            AcquireTokenByUserFederatedIdentityCredentialParameters userFicParameters)
            : base(serviceBundle, authenticationRequestParameters, userFicParameters)
        {
            _userFicParameters = userFicParameters;
        }

        protected override async Task<AuthenticationResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            await ResolveAuthorityAsync().ConfigureAwait(false);

            var additionalBodyParameters = GetAdditionalBodyParameters(_userFicParameters.Assertion);
            MsalTokenResponse msalTokenResponse = await SendTokenRequestAsync(additionalBodyParameters, cancellationToken).ConfigureAwait(false);

            return await CacheTokenResponseAndCreateAuthenticationResultAsync(msalTokenResponse, cancellationToken).ConfigureAwait(false);
        }

        private Dictionary<string, string> GetAdditionalBodyParameters(string assertion)
        {
            var dict = new Dictionary<string, string>
            {
                [OAuth2Parameter.GrantType] = OAuth2GrantType.UserFic,
                [OAuth2Parameter.Username] = _userFicParameters.Username,
                [OAuth2Parameter.UserFederatedIdentityCredential] = assertion
            };

            ISet<string> unionScope = new HashSet<string>
            {
                OAuth2Value.ScopeOpenId,
                OAuth2Value.ScopeOfflineAccess,
                OAuth2Value.ScopeProfile
            };

            unionScope.UnionWith(AuthenticationRequestParameters.Scope);
            dict[OAuth2Parameter.Scope] = unionScope.AsSingleString();
            dict[OAuth2Parameter.ClientInfo] = "1";

            return dict;
        }

        protected override KeyValuePair<string, string>? GetCcsHeader(IDictionary<string, string> additionalBodyParameters)
        {
            return GetCcsUpnHeader(_userFicParameters.Username);
        }
    }
}
