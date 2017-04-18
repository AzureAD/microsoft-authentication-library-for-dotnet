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
    public delegate void LogCallback(Logger.LogLevel level, string message, bool containsPii);

    /// <summary>
    /// MSAL Logger class that allows developers to configure log level, configure callbacks etc.
    /// </summary>
    public sealed class Logger
    {
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

        internal Logger(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        private Guid CorrelationId { get; set; }

        private static readonly object LockObj = new object();

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
        public static bool DefaultLoggingEnabled { get; set; } = true;

        internal static void ExecuteCallback(Logger.LogLevel level, string message, bool containsPii)
        {
            lock (LockObj)
            {
                _logCallback?.Invoke(level, message, containsPii);
            }
        }
        
        #region LogMessages
        /// <summary>
        /// Method for error logging
        /// </summary>
        internal void Error(string message)
        {
            LogMessage(message, LogLevel.Error, false);
        }

        /// <summary>
        /// Method for error logging of Pii 
        /// </summary>
        internal void ErrorPii(string message)
        {
            LogMessage(message, LogLevel.Error, true);
        }

        /// <summary>
        /// Method for warning logging
        /// </summary>
        internal void Warning(string message)
        {
            LogMessage(message, LogLevel.Warning, false);
        }

        /// <summary>
        /// Method for warning logging of Pii 
        /// </summary>
        internal void WarningPii(string message)
        {
            LogMessage(message, LogLevel.Warning, true);
        }

        /// <summary>
        /// Method for information logging
        /// </summary>
        internal void Info(string message)
        {
            LogMessage(message, LogLevel.Info, false);
        }

        /// <summary>
        /// Method for information logging for Pii
        /// </summary>
        internal void InfoPii(string message)
        {
            LogMessage(message, LogLevel.Info, true);
        }

        /// <summary>
        /// Method for verbose logging
        /// </summary>
        internal void Verbose(string message)
        {
            LogMessage(message, LogLevel.Verbose, false);
        }

        /// <summary>
        /// Method for verbose logging for Pii
        /// </summary>
        internal void VerbosePii(string message)
        {
            LogMessage(message, LogLevel.Verbose, true);
        }

        /// <summary>
        /// Method for error exception logging
        /// </summary>
        internal void Error(Exception ex)
        {
            Error(ex.ToString());
        }

        /// <summary>
        /// Method for error exception logging for Pii
        /// </summary>
        internal void ErrorPii(Exception ex)
        {
            ErrorPii(ex.ToString());
        }

        #endregion

        private void LogMessage(string logMessage, LogLevel logLevel, bool containsPii)
        {
            if ((logLevel > Level) || (!PiiLoggingEnabled && containsPii))
            {
                return;
            }

            //format log message;
            string correlationId = (CorrelationId.Equals(Guid.Empty))
                ? string.Empty
                : " - " + CorrelationId;

            string log = string.Format(CultureInfo.InvariantCulture, "MSAL {0} {1} {2} [{3}{4}] {5}",
                MsalIdHelper.GetMsalVersion(),
                PlatformPlugin.PlatformInformation.GetOperatingSystem(),
                MsalIdHelper.GetMsalIdParameters()[MsalIdParameter.OS], DateTime.UtcNow, correlationId, logMessage);

            if (DefaultLoggingEnabled)
            {
                PlatformPlugin.LogMessage(logLevel, log);
            }

            ExecuteCallback(logLevel, log, containsPii);
        }
    }
}