// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;

namespace CommonCache.Test.Common
{
    public static class CommandLineExecutor
    {
        public static void Execute(string[] args, Func<CommandLineOptions, Task> runFunc)
        {
            void SyncRunAction(CommandLineOptions options)
            {
                Console.WriteLine(Assembly.GetEntryAssembly().Location);
                try
                {
                    runFunc.Invoke(options).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    File.WriteAllText(options.ResultsFilePath, JsonConvert.SerializeObject(ExecutionContent.CreateFromException(ex)));
                }
            }

            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(SyncRunAction).WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.Error.WriteLine(error.ToString());
            }
        }
    }
}
