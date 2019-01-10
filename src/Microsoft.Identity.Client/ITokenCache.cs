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

using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This is the interface that implements the public access to cache operations.
    /// </summary>
    public interface ITokenCache
    {
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME
        /// <summary>
        /// 
        /// </summary>
        /// <param name="beforeAccess"></param>
        void SetBeforeAccess(TokenCache.TokenCacheNotification beforeAccess);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="afterAccess"></param>
        void SetAfterAccess(TokenCache.TokenCacheNotification afterAccess);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="beforeWrite"></param>
        void SetBeforeWrite(TokenCache.TokenCacheNotification beforeWrite);

        /// <summary>
        /// Unified Only
        /// </summary>
        /// <returns></returns>
        byte[] Serialize();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unifiedState"></param>
        void Deserialize(byte[] unifiedState);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        CacheData SerializeUnifiedAndAdalCache();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheData"></param>
        void DeserializeUnifiedAndAdalCache(CacheData cacheData);
#endif
    }
}