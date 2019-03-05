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

using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.Unit.Utils
{
    public class CacheProgram
    {
        public CacheProgram(string executablePath, string resultsFilePath, CacheStorageType cacheStorageType)
        {
            ExecutablePath = executablePath;
            ResultsFilePath = resultsFilePath;
            CacheStorageType = cacheStorageType;
        }

        public string ExecutablePath { get; }
        public string ResultsFilePath { get; }
        public CacheStorageType CacheStorageType { get; }

        public async Task<CacheProgramResults> ExecuteAsync(string username, string password, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.Append($"--userName {username} ");
            sb.Append($"--userPassword {password} ");
            sb.Append($"--resultsFilePath {ResultsFilePath.EncloseQuotes()} ");
            sb.Append($"--cacheStorageType {Convert.ToInt32(CacheStorageType, CultureInfo.InvariantCulture)} ");
            string arguments = sb.ToString();

            var processUtils = new ProcessUtils();

            try
            {
                var processRunResults = await processUtils.RunProcessAsync(ExecutablePath, arguments, cancellationToken).ConfigureAwait(false);
                return CacheProgramResults.CreateFromResultsFile(ResultsFilePath, processRunResults);
            }
            catch (ProcessRunException ex)
            {
                return CacheProgramResults.CreateWithFailedExecution(ex);
            }
        }
    }
}
