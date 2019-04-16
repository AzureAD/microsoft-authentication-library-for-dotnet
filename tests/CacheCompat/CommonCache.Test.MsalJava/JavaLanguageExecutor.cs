// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.MsalJava
{
    public class JavaLanguageExecutor : ILanguageExecutor
    {
        public JavaLanguageExecutor(string javaClassPath)
        {
            JavaClassPath = javaClassPath;
        }

        // Path to java class with a public static void main() function to execute
        public string JavaClassPath { get; }

        public async Task<ProcessRunResults> ExecuteAsync(
            string clientId,
            string authority,
            string scope,
            string username,
            string password,
            string cacheFilePath,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.Append($"{JavaClassPath.EncloseQuotes()} ");
            sb.Append($"{clientId} ");
            sb.Append($"{authority} ");
            sb.Append($"{scope} ");
            sb.Append($"{username} ");
            sb.Append($"{password} ");
            sb.Append($"{cacheFilePath.EncloseQuotes()} ");
            string arguments = sb.ToString();

            string executablePath = "java.exe";

            Console.WriteLine($"Calling:  {executablePath} {arguments}");

            var processUtils = new ProcessUtils();

            var processRunResults = await processUtils.RunProcessAsync(executablePath, arguments, cancellationToken).ConfigureAwait(false);
            return processRunResults;
        }
    }
}
