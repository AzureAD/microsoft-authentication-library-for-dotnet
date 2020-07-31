// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.MsalJava
{
    public class JavaLanguageExecutor : ILanguageExecutor
    {
        public JavaLanguageExecutor(string className)
        {
            ClassName = className;
        }

        public string ClassName { get; }

        public async Task<ProcessRunResults> ExecuteAsync(
            string arguments,
            CancellationToken cancellationToken)
        {
            var processUtils = new ProcessUtils();
            string executablePath = @"mvn.cmd"; // replace with std. devops build vm location

            string pomFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "pom.xml");

            executablePath = await processUtils.FindProgramAsync(executablePath, cancellationToken).ConfigureAwait(false);

            try
            {
                string compileArguments = $"-f \"{pomFilePath}\" compile";
                Console.WriteLine($"Calling:  {executablePath} {compileArguments}");
                var compileResults = await processUtils.RunProcessAsync(executablePath, compileArguments, cancellationToken).ConfigureAwait(false);
            }
            catch (ProcessRunException ex)
            {
                Console.WriteLine(ex.ProcessStandardOutput);
                throw;
            }

            string executeArguments = $"-f \"{pomFilePath}\" exec:java \"-Dexec.mainClass={ClassName}\" \"-Dexec.args={arguments}\"";

            Console.WriteLine($"Calling:  {executablePath} {executeArguments}");
            var processRunResults = await processUtils.RunProcessAsync(executablePath, executeArguments, cancellationToken).ConfigureAwait(false);
            return processRunResults;
        }
    }
}
