//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class Logger
    {
        internal static void Verbose(CallState callState, string arg, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            Verbose(callState, callerFilePath, "{0}", new object[] { arg });
        }

        internal static void Information(CallState callState, string arg, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            Information(callState, callerFilePath, "{0}", new object[] { arg });
        }

        internal static void Information(CallState callState, string format, object arg, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            Information(callState, callerFilePath, format, new [] { arg });
        }

        internal static void Information(CallState callState, string format, object arg, object arg2, object arg3, object arg4, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            Information(callState, callerFilePath, format, new [] { arg, arg2, arg3, arg4 });
        }

        internal static void Warning(CallState callState, string arg, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            Warning(callState, callerFilePath, "{0}", new object[] { arg });
        }

        internal static void Warning(CallState callState, string format, object arg, object arg2, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            Warning(callState, callerFilePath, format, new[] { arg, arg2 });
        }

    }
}