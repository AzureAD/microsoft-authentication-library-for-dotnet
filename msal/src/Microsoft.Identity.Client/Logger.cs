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
using System.Globalization;
using Microsoft.Identity.Client.Internal;
using Exception = System.Exception;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Callback delegate that allows the developer to consume logs handle them in a custom manner.
    /// </summary>
    /// <param name="level">Log level of the message</param>
    /// <param name="message">Pre-formatted log message</param>
    /// <param name="containsPii">Indicates if the log message contains PII. If Logger.PiiLoggingEnabled is set to 
    /// false then this value is always false.</param>
    public delegate void LogCallback(LogLevel level, string message, bool containsPii);

    /// <summary>
    /// MSAL Log Levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Error Log level
        /// </summary>
        Error = 0,

        /// <summary>
        /// Warning Log level
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Information Log level
        /// </summary>
        Info = 2,

        /// <summary>
        /// Verbose Log level
        /// </summary>
        Verbose = 3
    }

    /// <summary>
    /// MSAL logger settings class that allows developers to configure log level, configure callbacks etc.
    /// </summary>
    public sealed class Logger
    {
        internal static readonly object LockObj = new object();

        private static volatile LogCallback _logCallback;
        /// <summary>
        /// Callback instance that can be provided by the developer to consume and publish logs in a custom manner. 
        /// The property can only be set once and it will throw an ArgumentException if called twice.
        /// </summary>
        public static LogCallback LogCallback
        {
            set
            {
                lock (LockObj)
                {
                    if (_logCallback != null)
                    {
                        throw new ArgumentException("MSAL logging callback can only be set once per process and" +
                                                   " should never change once set.");
                    }

                    _logCallback = value;
                }
            }

            internal get { return _logCallback; }
        }

        /// <summary>
        /// Configurable log level. Default value is Info.
        /// </summary>
        public static LogLevel Level { get; set; } = LogLevel.Info;

        /// <summary>
        /// Flag to enable/disable logging of PII data. PII logs are never written to default outputs like Console, Logcat or NSLog.
        /// Default is set to false.
        /// </summary>
        public static bool PiiLoggingEnabled { get; set; } = false;

        /// <summary>
        /// Flag to enable/disable logging to platform defaults. In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
        /// In android, logcat is used.
        /// </summary>
        public static bool DefaultLoggingEnabled { get; set; } = false;
    }
}