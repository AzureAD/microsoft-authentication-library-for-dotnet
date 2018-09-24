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

using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;

namespace Microsoft.Identity.Core.Telemetry
{
    internal class TelemetryTokenCacheAccessor : TokenCacheAccessor
    {
        // The content of this class has to be placed outside of its base class TokenCacheAccessor,
        // otherwise we would have to modify multiple implementations of TokenCacheAccessor on different platforms.
        public void SaveAccessToken(MsalAccessTokenCacheItem item, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.AT }))
            {
                SaveAccessToken(item);
            }
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.RT }))
            {
                SaveRefreshToken(item);
            }
        }

        public void SaveIdToken(MsalIdTokenCacheItem item, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.ID }))
            {
                SaveIdToken(item);
            }
        }

        public void SaveAccount(MsalAccountCacheItem item, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.ACCOUNT }))
            {
                SaveAccount(item);
            }
        }

        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.AT }))
            {
                DeleteAccessToken(cacheKey);
            }
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.RT }))
            {
                DeleteRefreshToken(cacheKey);
            }
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.ID }))
            {
                DeleteIdToken(cacheKey);
            }
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey, RequestContext requestContext)
        {
            using (CoreTelemetryService.CreateTelemetryHelper(requestContext.TelemetryRequestId,
                new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.ACCOUNT }))
            {
                DeleteAccount(cacheKey);
            }
        }
    }
}
