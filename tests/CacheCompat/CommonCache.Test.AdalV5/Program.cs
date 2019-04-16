// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CommonCache.Test.AdalV5
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new AdalV5CacheExecutor().Execute(args);
        }

        private class AdalV5CacheExecutor : AbstractCacheExecutor
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
                    CommonCacheTestUtils.MsalV2CacheFilePath,
                    CommonCacheTestUtils.MsalV3CacheFilePath);

                var authenticationContext = new AuthenticationContext(app.Authority, tokenCache);

                var items = authenticationContext.TokenCache.ReadItems().ToList();
                Console.WriteLine("here come the cache items!: {0}", tokenCache.Count);
                foreach (var item in items)
                {
                    Console.WriteLine("here's a cache item!");
                    Console.WriteLine(item.DisplayableId);
                }

                Console.WriteLine("Calling ATS with username: {0}", options.Username);

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
