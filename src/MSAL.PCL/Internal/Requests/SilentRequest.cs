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
        public SilentRequest(AuthenticationRequestParameters authenticationRequestParameters, IPlatformParameters parameters, bool forceRefresh)
            : base(authenticationRequestParameters)
        {
            if (authenticationRequestParameters.User == null)
            {
                throw new ArgumentNullException(nameof(authenticationRequestParameters.User));
            }
            
            PlatformPlugin.BrokerHelper.PlatformParameters = parameters;
            this.SupportADFS = false;
            this.ForceRefresh = forceRefresh;
        }

        protected override void SetAdditionalRequestParameters(OAuth2Client client)
        {
            throw new System.NotImplementedException();
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
                RefreshTokenCacheItem refreshTokenItem =
                    TokenCache.FindRefreshToken(AuthenticationRequestParameters);

                if (refreshTokenItem == null)
                {
                    PlatformPlugin.Logger.Verbose(this.CallState, "No token matching arguments found in the cache");
                    throw new MsalSilentTokenAcquisitionException(
                        new Exception("No token matching arguments found in the cache"));
                }

                await this.RefreshAccessTokenAsync(refreshTokenItem).ConfigureAwait(false);
            }
        }



        internal async Task RefreshAccessTokenAsync(RefreshTokenCacheItem item)
        {
                PlatformPlugin.Logger.Verbose(this.CallState, "Refreshing access token...");
                
                    await this.SendTokenRequestByRefreshTokenAsync(item.RefreshToken).ConfigureAwait(false);

                    if (Response.IdToken == null)
                    {
                        // If Id token is not returned by token endpoint when refresh token is redeemed, 
                        // we should copy tenant and user information from the cached token.
                        Response.IdToken = item.RawIdToken;
                    }
        }
    }
}