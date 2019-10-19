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
            string executablePath = @"C:\apache-maven-3.6.2\bin\mvn.cmd";

            string pomFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "pom.xml");

            try
            {
                string findMavenArgs = $"mvn.cmd";
                Console.WriteLine($"Calling:  where {findMavenArgs}");
                var whereMavenResults = await processUtils.RunProcessAsync("where", findMavenArgs, cancellationToken).ConfigureAwait(false);
                File.WriteAllText(@"C:\Users\henrikm\AppData\Local\Temp\adalcachecompattestdata\mvnishere.txt", whereMavenResults.ToString());

            }
            catch (ProcessRunException ex)
            {
                Console.WriteLine(ex.ProcessStandardOutput);
                throw;
            }

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
