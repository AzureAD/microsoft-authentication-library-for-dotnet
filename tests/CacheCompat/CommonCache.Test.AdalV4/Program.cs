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

using System;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CommonCache.Test.AdalV4
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new AdalV4CacheExecutor().Execute(args);
        }

        private class AdalV4CacheExecutor : AbstractCacheExecutor
        {
            /// <inheritdoc />
            protected override async Task<CacheExecutorResults> InternalExecuteAsync(CommandLineOptions options)
            {
                var app = PreRegisteredApps.CommonCacheTestV1;
                string resource = PreRegisteredApps.MsGraph;

                LoggerCallbackHandler.LogCallback = (LogLevel level, string message, bool containsPii) =>
                {
                    Console.WriteLine("{0}: {1}", level, message);
                };

                CommonCacheTestUtils.EnsureCacheFileDirectoryExists();
                var tokenCache = new FileBasedTokenCache(
                    options.CacheStorageType,
                    CommonCacheTestUtils.AdalV3CacheFilePath,
                    CommonCacheTestUtils.MsalV2CacheFilePath);
                var authenticationContext = new AuthenticationContext(app.Authority, tokenCache);

                try
                {
                    var result = await authenticationContext.AcquireTokenSilentAsync(
                        resource, 
                        app.ClientId, 
                        new UserIdentifier(options.Username, UserIdentifierType.RequiredDisplayableId)).ConfigureAwait(false);

                    Console.WriteLine($"got token for '{result.UserInfo.DisplayableId}' from the cache");
                    return new CacheExecutorResults(result.UserInfo.DisplayableId, true);
                }
                catch (AdalSilentTokenAcquisitionException)
                {
                    var result = await authenticationContext.AcquireTokenAsync(
                                     resource,
                                     app.ClientId,
                                     new UserPasswordCredential(options.Username, options.UserPassword)).ConfigureAwait(false);

                    Console.WriteLine($"got token for '{result.UserInfo.DisplayableId}' without the cache");
                    return new CacheExecutorResults(result.UserInfo.DisplayableId, false);
                }
            }
        }
    }
}
