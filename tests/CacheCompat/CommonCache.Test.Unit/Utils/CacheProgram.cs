// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;
using Newtonsoft.Json;

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

        public async Task<CacheProgramResults> ExecuteAsync(IEnumerable<LabUserData> labUserData, CancellationToken cancellationToken)
        {
            var testInputData = new TestInputData
            {
                ResultsFilePath = ResultsFilePath,
                StorageType = CacheStorageType,
                LabUserDatas = labUserData.ToList()
            };

            var inputDataJson = JsonConvert.SerializeObject(testInputData);
            string inputFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            File.WriteAllText(inputFilePath, inputDataJson);

            var sb = new StringBuilder();
            sb.Append($"--inputPath {inputFilePath.EncloseQuotes()} ");
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
