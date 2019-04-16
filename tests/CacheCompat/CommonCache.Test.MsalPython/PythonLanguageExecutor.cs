// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.MsalPython
{
    public class PythonLanguageExecutor : ILanguageExecutor
    {
        public PythonLanguageExecutor(string pythonScriptPath)
        {
            PythonScriptPath = pythonScriptPath;
        }

        public string PythonScriptPath { get; }

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
            sb.Append($"{PythonScriptPath.EncloseQuotes()} ");
            sb.Append($"{clientId} ");
            sb.Append($"{authority} ");
            sb.Append($"{scope} ");
            sb.Append($"{username} ");
            sb.Append($"{password} ");
            sb.Append($"{cacheFilePath.EncloseQuotes()} ");
            string arguments = sb.ToString();

            string executablePath = "python.exe";

            Console.WriteLine($"Calling:  {executablePath} {arguments}");

            var processUtils = new ProcessUtils();

            var processRunResults = await processUtils.RunProcessAsync(executablePath, arguments, cancellationToken).ConfigureAwait(false);
            return processRunResults;
        }
    }
}
