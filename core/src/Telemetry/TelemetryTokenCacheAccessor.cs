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

using Microsoft.Identity.Client;
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
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.AT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                SaveAccessToken(item);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public void SaveRefreshToken(MsalRefreshTokenCacheItem item, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.RT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                SaveRefreshToken(item);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public void SaveIdToken(MsalIdTokenCacheItem item, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.ID };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                SaveIdToken(item);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public void SaveAccount(MsalAccountCacheItem item, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.ACCOUNT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                SaveAccount(item);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.AT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                DeleteAccessToken(cacheKey);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.RT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                DeleteRefreshToken(cacheKey);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public void DeleteIdToken(MsalIdTokenCacheKey cacheKey, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.ID };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                DeleteIdToken(cacheKey);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public void DeleteAccount(MsalAccountCacheKey cacheKey, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.ACCOUNT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                DeleteAccount(cacheKey);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }
    }
}
