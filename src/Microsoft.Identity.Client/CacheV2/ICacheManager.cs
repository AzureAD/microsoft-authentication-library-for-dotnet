// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.CacheV2
{
    /// <summary>
    /// This interface is for an individual request to access the cache functions.
    /// It is assumed that the implementation will have context about the call
    /// when using the cache manager.  In msal, this context means AuthenticationParameters.
    /// </summary>
    internal interface ICacheManager
    {
        /// <summary>
        /// Try to read the cache.  If a cache hit of any kind is found, return the token(s)
        /// and account information that was discovered.
        /// </summary>
        /// <param name="msalTokenResponse"></param>
        /// <param name="account"></param>
        /// <returns>True if a cache hit of any kind is found, False otherwise.</returns>
        bool TryReadCache(out MsalTokenResponse msalTokenResponse, out IAccount account);

        /// <summary>
        /// Given a MsalTokenResponse from the server, cache any relevant entries.
        /// </summary>
        /// <param name="tokenResponse"></param>
        /// <returns></returns>
        IAccount CacheTokenResponse(MsalTokenResponse tokenResponse);

        /// <summary>
        /// Delete the cached refresh token for this cache context.
        /// </summary>
        void DeleteCachedRefreshToken();
    }
}