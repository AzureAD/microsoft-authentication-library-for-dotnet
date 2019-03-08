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
                Console.WriteLine("PYTHON STDOUT");
                Console.WriteLine(stdout);
                Console.WriteLine();
                Console.WriteLine("PYTHON STDERR");
                Console.WriteLine(processRunResults.StandardError);
                Console.WriteLine();

                if (stdout.Contains("**TOKEN RECEIVED FROM CACHE**"))
                {
                    return new CacheExecutorResults(username, true);
                }
                else if (stdout.Contains("**TOKEN RECEIVED, BUT _NOT_ FROM CACHE**"))
                {
                    return new CacheExecutorResults(username, false);
                }
                else
                {
                    Console.WriteLine("NO TOKEN REPORTED AS RECEIVED!");
                    return new CacheExecutorResults(username, false);
                }
            }
            catch (ProcessRunException prex)
            {
                Console.WriteLine(prex.ProcessStandardOutput);
                Console.WriteLine(prex.ProcessStandardError);
                Console.WriteLine(prex.Message);
                return new CacheExecutorResults(string.Empty, false);
            }
        }
    }
}
