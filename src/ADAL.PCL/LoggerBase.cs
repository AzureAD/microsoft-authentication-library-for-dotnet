//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Globalization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal abstract class LoggerBase
    {
        internal abstract void Verbose(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "");

        internal abstract void Information(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "");

        internal abstract void Warning(CallState callState, string message, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "");

        internal abstract void Error(CallState callState, Exception ex, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "");

        internal static string GetCallerFilename(string callerFilePath)
        {
            return callerFilePath.Substring(callerFilePath.LastIndexOf("\\", StringComparison.Ordinal) + 1);
        }

        internal static string PrepareLogMessage(CallState callState, string classOrComponent, string message)
        {
            string correlationId = (callState != null) ? callState.CorrelationId.ToString() : string.Empty;
            return string.Format(CultureInfo.CurrentCulture, "{0}: {1} - {2}: {3}", DateTime.UtcNow, correlationId, classOrComponent, message);
        }
    }
}
