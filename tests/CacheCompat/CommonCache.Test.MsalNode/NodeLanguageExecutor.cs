// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.MsalPython
{
    public class NodeLanguageExecutor : ILanguageExecutor
    {
        public async Task<ProcessRunResults> ExecuteAsync(
            string arguments,
            CancellationToken cancellationToken)
        {
            var processUtils = new ProcessUtils();

            const string npm = "npm.cmd";
            string npmPath = await processUtils.FindProgramAsync(npm, cancellationToken).ConfigureAwait(false);

            string directoryWithScript = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NodeScript");

            try
            {
                var result = await processUtils.RunProcessAsync(npmPath, $"install {directoryWithScript}").ConfigureAwait(false);
            }            
            catch (ProcessRunException ex)
            {
                Console.WriteLine(ex.ProcessStandardOutput);
                throw;
            }

            string executablePath = "node.exe";

            executablePath = await processUtils.FindProgramAsync(executablePath, cancellationToken).ConfigureAwait(false);
            string executeArgs = $"{directoryWithScript}\\index.js {arguments}";

            Console.WriteLine($"Calling: {executablePath} {executeArgs}");
            ProcessRunResults processRunResults = await processUtils.RunProcessAsync(executablePath, executeArgs, cancellationToken).ConfigureAwait(false);
            return processRunResults;
        }
    }
}
