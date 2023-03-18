// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAMConsoleCallsIntoWAMLib
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var result = await WAMNetClassicLib
                .Broker
                .InvokeWAMAsync()
                .ConfigureAwait(false);
            
            Console.WriteLine(result);
        }
    }
}
