// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CommonCache.Test.Common
{
    public static class LanguageRunner
    {
        public static async Task<CacheExecutorResults> ExecuteAsync(
            ILanguageExecutor languageExecutor,
            string clientId,
            string authority,
            string scope,
            string username,
            string userPassword,
            string msalV3CacheFilePath,
            CancellationToken cancellationToken)
        {
            try
            {
                var processRunResults = await languageExecutor.ExecuteAsync(
                    clientId,
                    authority,
                    scope,
                    username,
                    userPassword,
                    msalV3CacheFilePath,
                    CancellationToken.None).ConfigureAwait(false);

                string stdout = processRunResults.StandardOut;

                Console.WriteLine();
                Console.WriteLine("STDOUT");
                Console.WriteLine(stdout);
                Console.WriteLine();
                Console.WriteLine("STDERR");
                Console.WriteLine(processRunResults.StandardError);
                Console.WriteLine();

                if (stdout.Contains("**TOKEN RECEIVED FROM CACHE**"))
                {
                    return new CacheExecutorResults();
                }
                else if (stdout.Contains("**TOKEN RECEIVED, BUT _NOT_ FROM CACHE**"))
                {
                    return new CacheExecutorResults();
                }
                else
                {
                    Console.WriteLine("NO TOKEN REPORTED AS RECEIVED!");
                    return new CacheExecutorResults();
                }
            }
            catch (ProcessRunException prex)
            {
                Console.WriteLine(prex.ProcessStandardOutput);
                Console.WriteLine(prex.ProcessStandardError);
                Console.WriteLine(prex.Message);
                return new CacheExecutorResults();
            }
        }
    }
}
