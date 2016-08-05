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

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// LogLevel
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// </summary>
        Information,

        /// <summary>
        /// </summary>
        Verbose,

        /// <summary>
        /// </summary>
        Warning,

        /// <summary>
        /// </summary>
        Error
    }

    /// <summary>
    /// </summary>
    public interface IMsalLogCallback
    {
        /// <summary>
        /// </summary>
        void Log(LogLevel level, string message);
    }

    /// <summary>
    /// </summary>
    public sealed class LoggerCallbackHandler
    {
        private static readonly object LockObj = new object();
        private static IMsalLogCallback _localCallback;

        /// <summary>
        /// </summary>
        public static IMsalLogCallback Callback
        {
            set
            {
                lock (LockObj)
                {
                    _localCallback = value;
                }
            }
        }

        internal static void ExecuteCallback(LogLevel level, string message)
        {
            lock (LockObj)
            {
                _localCallback?.Log(level, message);
            }
        }
    }
}