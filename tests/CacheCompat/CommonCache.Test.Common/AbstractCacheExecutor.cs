// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
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

        protected abstract Task<CacheExecutorResults> InternalExecuteAsync(CommandLineOptions options);

        private async Task ExecuteAsync(CommandLineOptions options)
        {
            try
            {
                var results = await InternalExecuteAsync(options).ConfigureAwait(false);
                WriteResultsFile(options.ResultsFilePath, ExecutionContent.CreateSuccess(results));
            }
            catch (Exception ex)
            {
                WriteResultsFile(options.ResultsFilePath, ExecutionContent.CreateFromException(ex));
            }
        }

        private void WriteResultsFile(string resultsFilePath, ExecutionContent results)
        {
            File.WriteAllText(resultsFilePath, JsonConvert.SerializeObject(results));
        }
    }
}
