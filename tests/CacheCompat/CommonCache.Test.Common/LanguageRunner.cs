// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CommonCache.Test.Common
{

    public static class LanguageRunner
    {
        public static async Task ExecuteAsync(
            ILanguageExecutor languageExecutor,
            TestInputData testInputData,
            CancellationToken cancellationToken)
        {
            try
            {

                string resource = TestInputData.MsGraph;
                string scope = resource + "/user.read";

                var languageTestInputData = new LanguageTestInputData(
                    testInputData, 
                    scope, 
                    CommonCacheTestUtils.MsalV3CacheFilePath);

                var inputDataJson = JsonConvert.SerializeObject(languageTestInputData);
                string inputFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                File.WriteAllText(inputFilePath, inputDataJson);

                var sb = new StringBuilder();
                sb.Append($"--inputPath {inputFilePath.EncloseQuotes()} ");
                string arguments = sb.ToString();

                var processRunResults = await languageExecutor.ExecuteAsync(arguments, cancellationToken).ConfigureAwait(false);

                string stdout = processRunResults.StandardOut;

                Console.WriteLine();
                Console.WriteLine("STDOUT");
                Console.WriteLine(stdout);
                Console.WriteLine();
                Console.WriteLine("STDERR");
                Console.WriteLine(processRunResults.StandardError);
                Console.WriteLine();
            }
            catch (ProcessRunException prex)
            {
                Console.WriteLine(prex.ProcessStandardOutput);
                Console.WriteLine(prex.ProcessStandardError);
                Console.WriteLine(prex.Message);
            }
        }
    }
}
