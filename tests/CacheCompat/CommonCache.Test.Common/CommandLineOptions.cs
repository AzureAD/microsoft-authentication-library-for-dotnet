// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using CommandLine;

namespace CommonCache.Test.Common
{
    [Flags]
    public enum CacheStorageType
    {
        None = 0,
        Adal = 1,
        MsalV2 = 2,
        MsalV3 = 4
    }

    public class CommandLineOptions
    {
        [Option("inputPath", Required = true, HelpText = "Path to input JSON (serialization of TestInputData class)")]
        public string InputFilePath { get; set; }
    }
}
