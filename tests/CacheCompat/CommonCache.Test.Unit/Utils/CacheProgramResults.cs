// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using CommonCache.Test.Common;
using Newtonsoft.Json;

namespace CommonCache.Test.Unit.Utils
{
    public class CacheProgramResults
    {
        private CacheProgramResults(ExecutionContent executionResults, bool processExecutionFailed, int processExitCode, string stdOut, string stdErr)
        {
            ExecutionResults = executionResults;
            ProcessExecutionFailed = processExecutionFailed;
            ProcessExitCode = processExitCode;
            StdOut = stdOut;
            StdErr = stdErr;
        }

        public ExecutionContent ExecutionResults { get; }
        public bool ProcessExecutionFailed { get; }
        public int ProcessExitCode { get; }
        public string StdOut { get; }
        public string StdErr { get; }

        public static CacheProgramResults CreateFromResultsFile(string resultsFilePath, ProcessRunResults processRunResults)
        {
            ExecutionContent executionResults;
            if (File.Exists(resultsFilePath))
            {
                executionResults = JsonConvert.DeserializeObject<ExecutionContent>(File.ReadAllText(resultsFilePath));
            }
            else
            {
                executionResults = new ExecutionContent
                {
                    IsError = true,
                    ErrorMessage = $"ResultsFilePath does not exist: {resultsFilePath}"
                };
            }

            return new CacheProgramResults(executionResults, false, 0, processRunResults.StandardOut, processRunResults.StandardError);
        }

        public static CacheProgramResults CreateWithFailedExecution(ProcessRunException prex)
        {
            return new CacheProgramResults(null, true, prex.ProcessExitCode, prex.ProcessStandardOutput, prex.ProcessStandardError);
        }
    }
}
