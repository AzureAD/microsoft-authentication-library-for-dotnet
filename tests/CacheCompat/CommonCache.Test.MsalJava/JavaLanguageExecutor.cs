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
            string arguments,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.Append($"{JavaClassPath.EncloseQuotes()} ");
            sb.Append(arguments);
            string finalArguments = sb.ToString();

            string executablePath = "java.exe";

            Console.WriteLine($"Calling:  {executablePath} {finalArguments}");
            var processUtils = new ProcessUtils();
            var processRunResults = await processUtils.RunProcessAsync(executablePath, finalArguments, cancellationToken).ConfigureAwait(false);
            return processRunResults;
        }
    }
}
