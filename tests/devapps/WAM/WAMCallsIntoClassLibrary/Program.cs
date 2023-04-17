// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAMCallsIntoClassLibrary
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await WAMClassLibrary.Authentication.InvokeBrokerAsync().ConfigureAwait(false);
        }
    }
}
