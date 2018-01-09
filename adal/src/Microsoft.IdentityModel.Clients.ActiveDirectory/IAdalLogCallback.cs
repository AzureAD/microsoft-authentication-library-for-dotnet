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
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// ADAL Log Levels
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Information log level
        /// </summary>
        Information,

        /// <summary>
        /// Verbose log level
        /// </summary>
        Verbose,

        /// <summary>
        /// Warning log level
        /// </summary>
        Warning,

        /// <summary>
        /// Error log level
        /// </summary>
        Error
    }

    /// <summary>
    /// Callback delegate that allows the developer to consume logs handle them in a custom manner.
    /// </summary>
    /// <param name="level">Log level of the message</param>
    /// <param name="message">Pre-formatted log message</param>
    /// <param name="containsPii">Indicates if the log message contains PII. If Logger.PiiLoggingEnabled is set to 
    /// false then this value is always false.</param>
    public delegate void LogCallback(LogLevel level, string message, bool containsPii);

    /// <summary>
    /// Obsolete Callback for capturing ADAL logs to custom logging schemes.
    /// Will be called only if LogCallback delegate is not set 
    /// and only for messages with no Pii
    /// </summary>
    [Obsolete("Use LogCallback delegate instead")]
    public interface IAdalLogCallback
    {
        /// <summary>
        /// Callback method to implement for custom logging
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">message to be logged</param>
        void Log(LogLevel level, string message);
    }

    /// <summary>
    /// This class is responsible for managing the callback state and its execution.
    /// </summary>
    public sealed class LoggerCallbackHandler
    {
        private static readonly object LockObj = new object();

        /// <summary>
        /// Flag to enable/disable logging of PII data. PII logs are never written to default outputs like Console, Logcat or NSLog.
        /// Default is set to false.
        /// </summary>
        public static bool PiiLoggingEnabled { get; set; } = false;

        /// <summary>
        /// Flag to control whether default logging should be performed in addition to calling
        /// the <see cref="Callback"/> handler (if any)
        /// </summary>
        public static bool UseDefaultLogging = true;

#pragma warning disable 0618
        private static IAdalLogCallback _localCallback;

        /// <summary>
        /// Obsolete Callback implementation
        /// Will be called only if LogCallback is not set 
        /// and only for messages with no Pii
        /// </summary>
        public static IAdalLogCallback Callback
#pragma warning restore 0618
        {
            set
            {
                lock (LockObj)
                {
                    _localCallback = value;
                }
            }

            internal get { return _localCallback; }
        }

        internal static void ExecuteObsoleteCallback(LogLevel level, string message)
        {
            lock (LockObj)
            {
                _localCallback?.Log(level, message);
            }
        }

        private static volatile LogCallback _logCallback;

        /// <summary>
        /// Instance of LogCallback delegate
        /// that can be provided by the developer to consume and publish logs in a custom manner.
        /// If set, Callback - instance of obsolete IAdalLogCallback will be ignored 
        /// </summary>
        public static LogCallback LogCallback
        {
            set
            {
                lock (LockObj)
                {
                    _logCallback = value;
                }
            }
            internal get { return _logCallback; }
        }

        internal static void ExecuteCallback(LogLevel level, string message, bool containsPii)
        {
            lock (LockObj)
            {
                _logCallback?.Invoke(level, message, containsPii);
            }
        }
    }
}