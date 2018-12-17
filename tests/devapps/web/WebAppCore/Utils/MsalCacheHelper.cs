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

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;

namespace WebAppCore.Utils
{
    public class MsalCacheHelper
    {
        private const string TokenCacheFileExtension = ".txt";
        private static readonly string TokenCacheDir = Startup.Configuration["TokenCacheDir"];

        public static TokenCache GetMsalSessionCacheInstance(ISession session, string cacheId)
        {
            var cache = new TokenCache();

            cache.SetBeforeAccess(delegate { LoadCacheFromSession(session, cacheId, cache); });

            cache.SetAfterAccess(
                args =>
                {
                    // if the access operation resulted in a cache update
                    if (args.HasStateChanged)
                    {
                        PersistCacheToSession(session, cacheId, cache);
                    }
                });

            LoadCacheFromSession(session, cacheId, cache);

            return cache;
        }

        private static void PersistCacheToSession(ISession session, string cacheId, TokenCache cache)
        {
            session.Set(cacheId, cache.Serialize());
        }

        private static void LoadCacheFromSession(ISession session, string cacheId, TokenCache cache)
        {
            cache.Deserialize(session.Get(cacheId));
        }

        public static TokenCache GetMsalFileCacheInstance(string cacheId)
        {
            var cache = new TokenCache();

            cache.SetBeforeAccess(delegate { LoadCacheFromFile(cacheId, cache); });

            cache.SetAfterAccess(
                args =>
                {
                    // if the access operation resulted in a cache update
                    if (args.HasStateChanged)
                    {
                        PersistCacheToFile(cacheId, cache);
                    }
                });

            LoadCacheFromFile(cacheId, cache);

            return cache;
        }

        private static void PersistCacheToFile(string cacheId, TokenCache cache)
        {
            string str = Encoding.UTF8.GetString(cache.Serialize());
            File.WriteAllText(TokenCacheDir + cacheId + TokenCacheFileExtension, str);
        }

        private static void LoadCacheFromFile(string cacheId, TokenCache cache)
        {
            string file = TokenCacheDir + cacheId + TokenCacheFileExtension;

            if (File.Exists(file))
            {
                string str = File.ReadAllText(file);
                cache.Deserialize(Encoding.UTF8.GetBytes(str));
            }
        }
    }
}