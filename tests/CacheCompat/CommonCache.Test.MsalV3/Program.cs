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
            protected override async Task<CacheExecutorResults> InternalExecuteAsync(TestInputData testInputData)
            {
                var v1App = PreRegisteredApps.CommonCacheTestV1;
                string resource = PreRegisteredApps.MsGraph;
                string[] scopes = new[]
                {
                    resource + "/user.read"
                };

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
                    testInputData.StorageType,
                    app.UserTokenCache,
                    CommonCacheTestUtils.AdalV3CacheFilePath,
                    CommonCacheTestUtils.MsalV2CacheFilePath,
                    CommonCacheTestUtils.MsalV3CacheFilePath);

                IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);

                var results = new CacheExecutorResults();

                foreach (var labUserData in testInputData.LabUserDatas)
                {
                    IAccount accountToReference = accounts.FirstOrDefault(x => x.Username.Equals(labUserData.Upn, StringComparison.OrdinalIgnoreCase));

                    try
                    {
                        var result = await app
                            .AcquireTokenSilent(scopes, accountToReference)
                            .WithAuthority(app.Authority)
                            .WithForceRefresh(false)
                            .ExecuteAsync(CancellationToken.None)
                            .ConfigureAwait(false);

                        Console.WriteLine($"got token for '{result.Account.Username}' from the cache");

                        results.AccountResults.Add(new CacheExecutorAccountResult(labUserData.Upn, result.Account.Username, true));
                    }
                    catch (MsalUiRequiredException)
                    {
                        var result = await app
                            .AcquireTokenByUsernamePassword(scopes, labUserData.Upn, labUserData.Password.ToSecureString())
                            .ExecuteAsync(CancellationToken.None)
                            .ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(result.AccessToken))
                        {
                            results.AccountResults.Add(new CacheExecutorAccountResult(labUserData.Upn, string.Empty, false));
                        }
                        else
                        {
                            Console.WriteLine($"got token for '{result.Account.Username}' without the cache");
                            results.AccountResults.Add(new CacheExecutorAccountResult(labUserData.Upn, result.Account.Username, false));
                        }
                    }
                }

                return results;
            }
        }
    }
}
