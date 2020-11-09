// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if iOS

using System;
using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class ConsolePlatformLogger : IPlatformLogger
    {
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
}
#endif
