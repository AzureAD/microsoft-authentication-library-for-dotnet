// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal.Credential
{
    internal sealed class AccessTokenClient
    {
        private readonly IHttpManager _http;
        private readonly ILoggerAdapter _log;

        public AccessTokenClient(IHttpManager http, ILoggerAdapter log)
        {
            _http = http;
            _log = log;
        }

        internal async Task<MsalTokenResponse> GetTokenAsync(
            X509Certificate2 cert,
            ManagedIdentityCredentialResponse cred,
            AuthenticationRequestParameters arp,
            CancellationToken ct)
        {
            var client = new OAuth2Client(_log, _http, cert);

            string scope = string.Join(" ", arp.Scope) + "/.default";
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.ClientCredentials);
            client.AddBodyParameter(OAuth2Parameter.Scope, scope);
            client.AddBodyParameter(OAuth2Parameter.ClientId, cred.ClientId);

            // add token_type only if caller asked for PoP
            if (arp.MtlsCertificate != null) //To Do need to add new APIs
            {
                client.AddBodyParameter(OAuth2Parameter.TokenType, Constants.MtlsPoPTokenType);
            }

            Uri tokenUri = new Uri(new Uri(cred.RegionalTokenUrl),
                                   $"{cred.TenantId}/oauth2/v2.0/token");

            return await client.GetTokenAsync(
                        tokenUri,
                        arp.RequestContext,
                        addCommonHeaders: true,
                        arp.OnBeforeTokenRequestHandler).ConfigureAwait(false);
        }
    }
}
