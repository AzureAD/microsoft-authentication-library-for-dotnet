// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
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

        private class MsalPythonCacheExecutor : AbstractLanguageCacheExecutor
        {
            protected override Task InternalExecuteAsync(TestInputData testInputData)
            {
                string scriptFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestMsalPython.py");

                return LanguageRunner.ExecuteAsync(
                    new PythonLanguageExecutor(scriptFilePath),
                    testInputData,
                    CancellationToken.None);
            }
        }
    }
}
