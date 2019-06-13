// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
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

        private class MsalJavaCacheExecutor : AbstractLanguageCacheExecutor
        {
            protected override Task InternalExecuteAsync(TestInputData testInputData)
            {
                return LanguageRunner.ExecuteAsync(
                    new JavaLanguageExecutor("TestConsoleApp"),
                    testInputData,
                    CancellationToken.None);
            }
        }
    }
}
