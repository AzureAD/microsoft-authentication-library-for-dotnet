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
using Android.Provider;
using Android.Util;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    [Android.Runtime.Preserve(AllMembers = true)]
    internal class Logger : LoggerBase
    {
        internal override void DefaultLog(LogLevel logLevel, string message)
        {
            switch (logLevel)
            {
                case LogLevel.Verbose:
                    Log.Verbose(null, message);
                    break;
                case LogLevel.Information:
                    Log.Info(null, message);
                    break;
                case LogLevel.Warning:
                    Log.Warn(null, message);
                    break;
                case LogLevel.Error:
                    Log.Error(null, message);
                    break;
            }
        }
    }
}

