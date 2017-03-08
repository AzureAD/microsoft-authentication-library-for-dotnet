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
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.Identity.Client.Internal.Cache;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class SilentRequest : BaseRequest
    {
        private RefreshTokenCacheItem _refreshTokenItem;

       public SilentRequest(AuthenticationRequestParameters authenticationRequestParameters, bool forceRefresh)
            : base(authenticationRequestParameters)
        {
            if (authenticationRequestParameters.User == null)
            {
                throw new ArgumentNullException(nameof(authenticationRequestParameters.User));
            }
            
            PlatformPlugin.BrokerHelper.PlatformParameters = authenticationRequestParameters.PlatformParameters;
            this.ForceRefresh = forceRefresh;
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            client.AddBodyParameter(OAuth2Parameter.GrantType, OAuth2GrantType.RefreshToken);
            client.AddBodyParameter(OAuth2Parameter.RefreshToken, _refreshTokenItem.RefreshToken);
        }

        protected override async Task SendTokenRequestAsync()
        {
            if (!this.LoadFromCache)
            {
                throw new MsalSilentTokenAcquisitionException(new Exception("Token cache is set to null"));
            }

            //look for access token first because force refresh is not set
            if (!ForceRefresh)
            {
                AccessTokenItem
                     = TokenCache.FindAccessToken(AuthenticationRequestParameters);
            }

            if (AccessTokenItem == null)
            {
                _refreshTokenItem =
                    TokenCache.FindRefreshToken(AuthenticationRequestParameters);

                if (_refreshTokenItem == null)
                {
                    RequestContext.MsalLogger.Verbose("No token matching arguments found in the cache");
                    throw new MsalSilentTokenAcquisitionException(
                        new Exception("No token matching arguments found in the cache"));
                }

                RequestContext.MsalLogger.Verbose("Refreshing access token...");
                await base.SendTokenRequestAsync().ConfigureAwait(false);

                if (Response.IdToken == null)
                {
                    // If Id token is not returned by token endpoint when refresh token is redeemed, 
                    // we should copy tenant and user information from the cached token.
                    Response.IdToken = _refreshTokenItem.RawIdToken;
                }

                if (Response.RefreshToken == null)
                {
                    Response.RefreshToken = _refreshTokenItem.RefreshToken;
                    RequestContext.MsalLogger.Info("Refresh token was missing from the token refresh response, so the refresh token in the request is returned instead");
                }
            }
        }
    }
}