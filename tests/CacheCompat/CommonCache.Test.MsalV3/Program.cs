// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;

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
            /// <inheritdoc/>
            protected override async Task<IEnumerable<CacheExecutorAccountResult>> InternalExecuteAsync(TestInputData testInputData)
            {
                string resource = TestInputData.MsGraph;
                string[] scopes = new[]
                {
                    resource + "/user.read"
                };

                var results = new List<CacheExecutorAccountResult>();

                foreach (var labUserData in testInputData.LabUserDatas)
                {
                    var app = PublicClientApplicationBuilder
                       .Create(labUserData.ClientId)
                       .WithAuthority(new Uri(labUserData.Authority), true)
                       .WithLogging((LogLevel level, string message, bool containsPii) =>
                       {
                           Console.WriteLine("{0}: {1}", level, message);
                       })
                       .Build();

                    FileBasedTokenCacheHelper.ConfigureUserCache(
                        testInputData.StorageType,
                        app.UserTokenCache,
                        CommonCacheTestUtils.MsalV2CacheFilePath,
                        CommonCacheTestUtils.MsalV3CacheFilePath);

                    IEnumerable<IAccount> accounts = await app.GetAccountsAsync().ConfigureAwait(false);

                    IAccount accountToReference = accounts.FirstOrDefault(x => x.Username.Equals(labUserData.Upn, StringComparison.OrdinalIgnoreCase));

                    try
                    {
                        var result = await app
                            .AcquireTokenSilent(scopes, accountToReference)                            
                            .WithForceRefresh(false)
                            .ExecuteAsync(CancellationToken.None)
                            .ConfigureAwait(false);

                        Console.WriteLine($"got token for '{result.Account.Username}' from the cache");

                        results.Add(new CacheExecutorAccountResult(labUserData.Upn, result.Account.Username, true));
                    }
                    catch (MsalUiRequiredException)
                    {
                        AuthenticationResult result;
                        if (DelegatedTokenFixtureProvider.TryGetRefreshToken(labUserData.ClientId, labUserData.Upn, out string refreshToken))
                        {
                            result = await ((IByRefreshToken)app).AcquireTokenByRefreshToken(scopes, refreshToken)
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                        else if (string.Equals(Environment.GetEnvironmentVariable("MSAL_CACHECOMPAT_DEVICE_CODE"), "1", StringComparison.Ordinal))
                        {
                            result = await app
                                .AcquireTokenWithDeviceCode(scopes, code =>
                                {
                                    Console.WriteLine(code.Message);
                                    return Task.CompletedTask;
                                })
                                .ExecuteAsync(CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            throw new InvalidOperationException("Provision MSAL_V5_DELEGATED_TOKEN_FIXTURE for the exact client and user, or explicitly enable device-code setup with MSAL_CACHECOMPAT_DEVICE_CODE=1.");
                        }

                        if (result.Account is null ||
                            !string.Equals(result.Account.Username, labUserData.Upn, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("Delegated setup signed in a different user from the requested cache-compatibility identity.");
                        }

                        if (string.IsNullOrWhiteSpace(result.AccessToken))
                        {
                            throw new InvalidOperationException("Delegated setup did not return an access token.");
                        }

                        Console.WriteLine($"got token for '{result.Account.Username}' without the cache");
                        results.Add(new CacheExecutorAccountResult(labUserData.Upn, result.Account.Username, false));
                    }
                }

                return results;
            }
        }
    }
}
