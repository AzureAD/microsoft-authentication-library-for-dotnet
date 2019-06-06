// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CommonCache.Test.Common
{
    public abstract class AbstractCacheExecutor
    {
        public void Execute(string[] args)
        {
            CommandLineExecutor.Execute(args, ExecuteAsync);
        }

        protected abstract Task<IEnumerable<CacheExecutorAccountResult>> InternalExecuteAsync(TestInputData testInputData);

        private async Task ExecuteAsync(TestInputData testInputData)
        {
            try
            {
                CommonCacheTestUtils.EnsureCacheFileDirectoryExists();

                var results = await InternalExecuteAsync(testInputData).ConfigureAwait(false);
                WriteResultsFile(testInputData.ResultsFilePath, ExecutionContent.CreateSuccess(results));
            }
            catch (Exception ex)
            {
                WriteResultsFile(testInputData.ResultsFilePath, ExecutionContent.CreateFromException(ex));
            }
        }

        private void WriteResultsFile(string resultsFilePath, ExecutionContent results)
        {
            File.WriteAllText(resultsFilePath, JsonConvert.SerializeObject(results));
        }
    }
}
