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

    // ReSharper disable once ClassNeverInstantiated.Global
    public class CommandLineOptions
    {
        [Option("inputPath", Required = true, HelpText = "Path to input JSON (serialization of TestInputData class)")]
        public string InputFilePath { get; set; }

        //[Option("resultsFilePath", Required = true, HelpText = "Path to write output results file.")]
        //public string ResultsFilePath { get; set; }

        //[Option("userName", Required = true, HelpText = "Username to login with.")]
        //public string Username{ get; set; }

        //[Option("userPassword", Required = true, HelpText = "Password to login with.")]
        //public string UserPassword { get; set; }

        //[Option("cacheStorageType", Required = true, HelpText = "Cache storage type(s) supported.")]
        //public int CacheStorageTypeInt {get; set;}

        //public CacheStorageType CacheStorageType => (CacheStorageType)CacheStorageTypeInt;
    }
}
