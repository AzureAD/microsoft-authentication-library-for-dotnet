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

namespace Microsoft.Identity.Client.Internal.Telemetry
{
    internal class TelemetryTokenCacheAccessor : TokenCacheAccessor
    {
        // The content of this class has to be placed outside of its base class TokenCacheAccessor,
        // otherwise we would have to modify multiple implementations of TokenCacheAccessor on different platforms.
        public new void SaveAccessToken(string cacheKey, string item, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.AT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                SaveAccessToken(cacheKey, item, requestContext);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public new void SaveRefreshToken(string cacheKey, string item, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheWrite) { TokenType = CacheEvent.TokenTypes.RT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                SaveRefreshToken(cacheKey, item, requestContext);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public new void DeleteAccessToken(string cacheKey, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.AT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                DeleteAccessToken(cacheKey, requestContext);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }

        public new void DeleteRefreshToken(string cacheKey, RequestContext requestContext)
        {
            var cacheEvent = new CacheEvent(CacheEvent.TokenCacheDelete) { TokenType = CacheEvent.TokenTypes.RT };
            Client.Telemetry.GetInstance().StartEvent(requestContext.TelemetryRequestId, cacheEvent);
            try
            {
                DeleteRefreshToken(cacheKey, requestContext);
            }
            finally
            {
                Client.Telemetry.GetInstance().StopEvent(requestContext.TelemetryRequestId, cacheEvent);
            }
        }
    }
}
