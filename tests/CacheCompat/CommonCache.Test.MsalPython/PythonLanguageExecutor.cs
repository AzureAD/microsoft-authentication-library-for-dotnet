// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
            string arguments,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.Append($"{PythonScriptPath.EncloseQuotes()} ");
            sb.Append(arguments);
            string finalArguments = sb.ToString();

            string executablePath = "python.exe";

            var processUtils = new ProcessUtils();
            executablePath = await processUtils.FindProgramAsync(executablePath, cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"Calling:  {executablePath} {finalArguments}");
            ProcessRunResults processRunResults = await processUtils.RunProcessAsync(executablePath, finalArguments, cancellationToken).ConfigureAwait(false);
            return processRunResults;
        }
    }
}
