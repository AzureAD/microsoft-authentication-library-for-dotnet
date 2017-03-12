//------------------------------------------------------------------------------
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

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Interface for callback to be implemented and provided by the developer.
    /// </summary>
    public interface IMsalLogCallback
    {
        /// <summary>
        /// Way for developer to register a callback
        /// level - loggging level of the message
        /// message - log message according to the log message format
        /// containsPii - whether the log message contains personally identifiable information (Pii)
        /// </summary>
        void Log(Logger.LogLevel level, string message, bool containsPii);
    }

    /// <summary>
    /// Class to consume developer provided callback.
    /// </summary>
    public sealed class LoggerCallbackHandler
    {
        private static readonly object LockObj = new object();
        private static IMsalLogCallback _localCallback;

        /// <summary>
        /// Callback instance
        /// </summary>
        public static IMsalLogCallback Callback
        {
            set
            {
                lock (LockObj)
                {
                    if (_localCallback != null )
                    {
                        throw new Exception("MSAL logging callback can only be set once per process and should never change once set.");
                    }
                    _localCallback = value;
                }
            }
        }

        internal static void ExecuteCallback(Logger.LogLevel level, string message, bool containsPii)
        {
            lock (LockObj)
            {
                _localCallback?.Log(level, message, containsPii);
            }
        }
    }
}