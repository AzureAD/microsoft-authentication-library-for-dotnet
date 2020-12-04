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
            new MsalNodeCacheExecutor().Execute(args);
        }

        private class MsalNodeCacheExecutor : AbstractLanguageCacheExecutor
        {
            protected override Task InternalExecuteAsync(TestInputData testInputData)
            {
                return LanguageRunner.ExecuteAsync(
                    new NodeLanguageExecutor(),
                    testInputData,
                    default);
            }
        }
    }
}
