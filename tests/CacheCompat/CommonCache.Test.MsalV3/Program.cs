// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.Identity.Client;

namespace CommonCache.Test.MsalV2
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new MsalV3CacheExecutor().Execute(args);
        }

        private class MsalV3CacheExecutor : AbstractCacheExecutor
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

                CommonCacheTestUtils.EnsureCacheFileDirectoryExists();

                var app = PublicClientApplicationBuilder
                    .Create(v1App.ClientId)
                    .WithAuthority(new Uri(v1App.Authority), true)
                    .WithLogging((LogLevel level, string message, bool containsPii) =>
                    {
                        Console.WriteLine("{0}: {1}", level, message);
                    })
                    .WithTelemetry(new TraceTelemetryConfig())
                    .Build();

                FileBasedTokenCacheHelper.ConfigureUserCache(
                    options.CacheStorageType,
                    app.UserTokenCache,
                    CommonCacheTestUtils.AdalV3CacheFilePath,
                    CommonCacheTestUtils.MsalV2CacheFilePath,
                    CommonCacheTestUtils.MsalV3CacheFilePath);

                IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);
                try
                {
                    var result = await app
                        .AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                        .WithAuthority(app.Authority)
                        .WithForceRefresh(false)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Console.WriteLine($"got token for '{result.Account.Username}' from the cache");
                    return new CacheExecutorResults(result.Account.Username, true);
                }
                catch (MsalUiRequiredException)
                {
                    var result = await app
                        .AcquireTokenByUsernamePassword(scopes, options.Username, options.UserPassword.ToSecureString())
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Console.WriteLine($"got token for '{result.Account.Username}' without the cache");
                    return new CacheExecutorResults(result.Account.Username, false);
                }
            }
        }
    }
}
