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

using System.IO;
using CommonCache.Test.Common;
using Microsoft.Identity.Json;

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
            var executionResults = JsonConvert.DeserializeObject<ExecutionContent>(File.ReadAllText(resultsFilePath));
            return new CacheProgramResults(executionResults, false, 0, processRunResults.StandardOut, processRunResults.StandardError);
        }

        public static CacheProgramResults CreateWithFailedExecution(ProcessRunException prex)
        {
            return new CacheProgramResults(null, true, prex.ProcessExitCode, prex.ProcessStandardOutput, prex.ProcessStandardError);
        }
    }
}