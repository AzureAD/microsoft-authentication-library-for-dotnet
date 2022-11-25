// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;

namespace Microsoft.Identity.Client.Utils
{
    internal class TraceWrapper
    {
        public static void WriteLine(string message) 
        {
#if !NETSTANDARD && !WINDOWS_APP
            Trace.WriteLine(message);
#endif
        }

    }
}
