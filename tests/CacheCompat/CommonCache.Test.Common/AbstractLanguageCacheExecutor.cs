// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

namespace CommonCache.Test.Common
{
    public abstract class AbstractLanguageCacheExecutor
    {
        public void Execute(string[] args)
        {
            CommandLineExecutor.Execute(args, ExecuteAsync);
        }

        protected abstract Task InternalExecuteAsync(TestInputData testInputData);

        private async Task ExecuteAsync(TestInputData testInputData)
        {
            try
            {
                CommonCacheTestUtils.EnsureCacheFileDirectoryExists();

                // The default behavior for the external language execution is for them to write the results file.
                await InternalExecuteAsync(testInputData).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                File.WriteAllText(testInputData.ResultsFilePath, JsonSerializer.Serialize(ExecutionContent.CreateFromException(ex)));
            }
        }
    }
}
