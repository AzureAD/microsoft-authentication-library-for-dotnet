// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.Identity.Client;

namespace CommonCache.Test.MsalV2
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new MsalV2CacheExecutor().Execute(args);
        }

        private class MsalV2CacheExecutor : AbstractCacheExecutor
        {
            /// <inheritdoc />
            protected override async Task<CacheExecutorResults> InternalExecuteAsync(CommandLineOptions options)
            {
                var v1App = PreRegisteredApps.CommonCacheTestV1;
                string resource = PreRegisteredApps.MsGraph;
                string[] scopes = new[]
                {
                    resource + "/user.read"
                };

                Logger.LogCallback = (LogLevel level, string message, bool containsPii) =>
                {
                    Console.WriteLine("{0}: {1}", level, message);
                };

                CommonCacheTestUtils.EnsureCacheFileDirectoryExists();

                var tokenCache = new TokenCache();

                FileBasedTokenCacheHelper.ConfigureUserCache(
                    options.CacheStorageType,
                    tokenCache,
                    CommonCacheTestUtils.AdalV3CacheFilePath,
                    CommonCacheTestUtils.MsalV2CacheFilePath);

                var app = new PublicClientApplication(v1App.ClientId, v1App.Authority, tokenCache)
                {
                    ValidateAuthority = true
                };

                IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                try
                {
                    var result = await app.AcquireTokenSilentAsync(scopes, accounts.FirstOrDefault(), app.Authority, false).ConfigureAwait(false);
                    Console.WriteLine($"got token for '{result.Account.Username}' from the cache");
                    return new CacheExecutorResults(result.Account.Username, true);
                }
                catch (MsalUiRequiredException)
                {
                    var result = await app.AcquireTokenByUsernamePasswordAsync(scopes, options.Username, options.UserPassword.ToSecureString()).ConfigureAwait(false);
                    Console.WriteLine($"got token for '{result.Account.Username}' without the cache");
                    return new CacheExecutorResults(result.Account.Username, false);
                }
            }
        }
    }
}
