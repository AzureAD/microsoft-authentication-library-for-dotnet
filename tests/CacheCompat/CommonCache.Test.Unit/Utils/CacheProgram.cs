// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
