// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    #if !WINDOWS_APP
    class ConsolePlatformLogger : IPlatformLogger
    {
        public void Always(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(string message)
        {
            Console.WriteLine(message);
        }

        public void Warning(string message)
        {
            Console.WriteLine(message);
        }

        public void Verbose(string message)
        {
            Console.WriteLine(message);
        }

        public void Information(string message)
        {
            Console.WriteLine(message);
        }
    }
    #endif
}
