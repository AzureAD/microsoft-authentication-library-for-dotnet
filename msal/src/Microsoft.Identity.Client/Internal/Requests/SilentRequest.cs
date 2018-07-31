//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentRequest : RequestBase
    {
        private MsalRefreshTokenCacheItem _msalRefreshTokenItem;

        public SilentRequest(AuthenticationRequestParameters authenticationRequestParameters, bool forceRefresh)
            : base(authenticationRequestParameters)
        {
            if (authenticationRequestParameters.User == null)
            {
                throw new MsalUiRequiredException(MsalUiRequiredException.UserNullError, "Null user was passed in AcquiretokenSilent API. Pass in " +
                                                                                         "a user object or call acquireToken authenticate.");
            }

            ForceRefresh = forceRefresh;
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken);
            client.AddBodyParameter(OAuth2Parameter.RefreshToken, _msalRefreshTokenItem.Secret);
        }

        internal override async Task PreTokenRequest()
        {
            if (!LoadFromCache)
            {
                throw new MsalUiRequiredException(MsalUiRequiredException.TokenCacheNullError,
                    "Token cache is set to null. Silent requests cannot be executed.");
            }

            //look for access token.
            MsalAccessTokenItem
                = await TokenCache.FindAccessToken(AuthenticationRequestParameters).ConfigureAwait(false);
            if (MsalAccessTokenItem != null)
            {
                MsalIdTokenItem = TokenCache.GetIdTokenCacheItem(MsalAccessTokenItem.GetIdTokenItemKey(), AuthenticationRequestParameters.RequestContext);
            }

            if (ForceRefresh)
            {
                MsalAccessTokenItem = null;
            }

            await CompletedTask.ConfigureAwait(false);
        }

        protected override async Task SendTokenRequestAsync()
        {
            if (MsalAccessTokenItem == null)
            {
                _msalRefreshTokenItem =
                    await TokenCache.FindRefreshToken(AuthenticationRequestParameters).ConfigureAwait(false);

                if (_msalRefreshTokenItem == null)
                {
                    const string msg = "No Refresh Token was found in the cache";
                    AuthenticationRequestParameters.RequestContext.Logger.Verbose(msg);
                    AuthenticationRequestParameters.RequestContext.Logger.VerbosePii(msg);

                    throw new MsalUiRequiredException(MsalUiRequiredException.NoTokensFoundError,
                        "No Refresh Token found in the cache");
                }

                AuthenticationRequestParameters.RequestContext.Logger.Verbose("Refreshing access token...");
                await ResolveAuthorityEndpoints().ConfigureAwait(false);
                await base.SendTokenRequestAsync().ConfigureAwait(false);

                if (Response.RefreshToken == null)
                {
                    Response.RefreshToken = _msalRefreshTokenItem.Secret;
                    const string msg = "Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead";
                    AuthenticationRequestParameters.RequestContext.Logger.Info(msg);
                    AuthenticationRequestParameters.RequestContext.Logger.InfoPii(msg);
                }
            }
        }
    }
}