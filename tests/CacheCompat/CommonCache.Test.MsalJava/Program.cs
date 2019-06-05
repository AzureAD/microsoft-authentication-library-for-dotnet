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
                // TODO: figure out how we setup the public main program, compile it from .java to .class, and execute it
                // May need to have the JavaLanguageExecutor take a .java file and then have a separate compile
                // step on that class to build the java code and run it.
                string javaClassFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SomeJavaClass.class");

                return LanguageRunner.ExecuteAsync(
                    new JavaLanguageExecutor(javaClassFilePath),
                    testInputData,
                    CancellationToken.None);
            }
        }
    }
}
