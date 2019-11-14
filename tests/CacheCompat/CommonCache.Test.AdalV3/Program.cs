// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CommonCache.Test.AdalV3
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new AdalV3CacheExecutor().Execute(args);
        }

        private class AdalV3CacheExecutor : AbstractCacheExecutor
        {
            /// <inheritdoc />
            protected override async Task<IEnumerable<CacheExecutorAccountResult>> InternalExecuteAsync(TestInputData testInputData)
            {
                LoggerCallbackHandler.LogCallback = (LogLevel level, string message, bool containsPii) =>
                {
                    Console.WriteLine("{0}: {1}", level, message);
                };


                var results = new List<CacheExecutorAccountResult>();

                foreach (var labUserData in testInputData.LabUserDatas)
                {
                    var tokenCache = new FileBasedAdalV3TokenCache(CommonCacheTestUtils.AdalV3CacheFilePath);
                    var authenticationContext = new AuthenticationContext(labUserData.Authority, tokenCache);

                    try
                    {
                        var result = await authenticationContext.AcquireTokenSilentAsync(
                            TestInputData.MsGraph,
                            labUserData.ClientId,
                            new UserIdentifier(labUserData.Upn, UserIdentifierType.RequiredDisplayableId)).ConfigureAwait(false);

                        Console.WriteLine($"got token for '{result.UserInfo.DisplayableId}' from the cache");
                        results.Add(new CacheExecutorAccountResult(
                            labUserData.Upn,
                            result.UserInfo.DisplayableId,
                            true));
                    }
                    catch (AdalSilentTokenAcquisitionException)
                    {
                        var result = await authenticationContext.AcquireTokenAsync(
                            TestInputData.MsGraph,
                            labUserData.ClientId,
                            new UserPasswordCredential(labUserData.Upn, labUserData.Password)).ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(result.AccessToken))
                        {
                            results.Add(new CacheExecutorAccountResult(labUserData.Upn, string.Empty, false));
                        }
                        else
                        {
                            Console.WriteLine($"got token for '{result.UserInfo.DisplayableId}' without the cache");
                            results.Add(new CacheExecutorAccountResult(
                                labUserData.Upn,
                                result.UserInfo.DisplayableId,
                                false));
                        }
                    }
                }

                return results;
            }
        }
    }
}
