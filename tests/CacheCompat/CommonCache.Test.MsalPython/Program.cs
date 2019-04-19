// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.MsalPython
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new MsalPythonCacheExecutor().Execute(args);
        }

        private class MsalPythonCacheExecutor : AbstractCacheExecutor
        {
            protected override async Task<CacheExecutorResults> InternalExecuteAsync(TestInputData testInputData)
            {
                var v1App = PreRegisteredApps.CommonCacheTestV1;
                string resource = PreRegisteredApps.MsGraph;
                string scope = resource + "/user.read";

                string scriptFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestMsalPython.py");

                return await LanguageRunner.ExecuteAsync(
                    new PythonLanguageExecutor(scriptFilePath),
                    v1App.ClientId,
                        v1App.Authority,
                        scope,
                        testInputData.LabUserDatas.First().Upn,
                        testInputData.LabUserDatas.First().Password,
                        CommonCacheTestUtils.MsalV3CacheFilePath,
                        CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
