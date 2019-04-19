// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommonCache.Test.Common;

namespace CommonCache.Test.MsalJava
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            new MsalJavaCacheExecutor().Execute(args);
        }

        private class MsalJavaCacheExecutor : AbstractCacheExecutor
        {
            protected override async Task<CacheExecutorResults> InternalExecuteAsync(TestInputData testInputData)
            {
                var v1App = PreRegisteredApps.CommonCacheTestV1;
                string resource = PreRegisteredApps.MsGraph;
                string scope = resource + "/user.read";

                // TODO: figure out how we setup the public main program, compile it from .java to .class, and execute it
                // May need to have the JavaLanguageExecutor take a .java file and then have a separate compile
                // step on that class to build the java code and run it.
                string javaClassFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SomeJavaClass.class");

                return await LanguageRunner.ExecuteAsync(
                    new JavaLanguageExecutor(javaClassFilePath),
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
