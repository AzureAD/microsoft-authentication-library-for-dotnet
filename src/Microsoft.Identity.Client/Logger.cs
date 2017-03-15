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

namespace Microsoft.Identity.Client
{
    public sealed class Logger
    {
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
            this.CorrelationId = correlationId;
        }

        private Guid CorrelationId { get; set; }

        /// <summary>
        /// Callback instance
        /// </summary>
        private static readonly object LockObj = new object();
        private static volatile ILoggerCallback _localCallback;

        public static ILoggerCallback Callback
        {
            set
            {
                lock (LockObj)
                {
                    if (_localCallback != null)
                    {
                        throw new Exception("MSAL logging callback can only be set once per process and" +
                                                   "should never change once set.");
                    }

                    _localCallback = value;
                }
            }
        }

        /// <summary>
        /// The default log level is set to info.
        /// </summary>
        public LogLevel Level { get; set; } = LogLevel.Info;

        /// <summary>
        /// Pii logging default is set to false
        /// </summary>
        public static bool PiiLoggingEnabled { get; set; } = false;

        internal static void ExecuteCallback(Logger.LogLevel level, string message, bool containsPii)
        {
            lock (LockObj)
            {
                _localCallback?.Log(level, message, containsPii);
            }
        }

        #region LogMessages
        /// <summary>
        /// Method for error logging
        /// </summary>
        public void Error(string message)
        {
            LogMessage(message, LogLevel.Error, false);
        }

        /// <summary>
        /// Method for error logging of Pii 
        /// </summary>
        public void ErrorPii(string message)
        {
            LogMessage(message, LogLevel.Error, true);
        }

        /// <summary>
        /// Method for warning logging
        /// </summary>
        public void Warning(string message)
        {
            LogMessage(message, LogLevel.Warning, false);
        }

        /// <summary>
        /// Method for warning logging of Pii 
        /// </summary>
        public void WarningPii(string message)
        {
            LogMessage(message, LogLevel.Warning, true);
        }

        /// <summary>
        /// Method for information logging
        /// </summary>
        public void Info(string message)
        {
            LogMessage(message, LogLevel.Info, false);
        }

        /// <summary>
        /// Method for information logging for Pii
        /// </summary>
        public void InfoPii(string message)
        {
            LogMessage(message, LogLevel.Info, true);
        }

        /// <summary>
        /// Method for verbose logging
        /// </summary>
        public void Verbose(string message)
        {
            LogMessage(message, LogLevel.Verbose, false);
        }

        /// <summary>
        /// Method for verbose logging for Pii
        /// </summary>
        public void VerbosePii(string message)
        {
            LogMessage(message, LogLevel.Verbose, true);
        }

        /// <summary>
        /// Method for error exception logging
        /// </summary>
        public void Error(Exception ex)
        {
            Error(ex.ToString());
        }

        /// <summary>
        /// Method for error exception logging for Pii
        /// </summary>
        public void ErrorPii(Exception ex)
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
                ? String.Empty
                : " - " + CorrelationId.ToString();

            string log = string.Format(CultureInfo.InvariantCulture, "MSAL {0} {1} {2} [{3}{4}] {5}",
                MsalIdHelper.GetMsalVersion(),
                PlatformPlugin.PlatformInformation.GetOperatingSystem(),
                MsalIdParameter.OS, DateTime.UtcNow, correlationId, logMessage);

            PlatformPlugin.LogMessage(logLevel, log);

            ExecuteCallback(logLevel, log, containsPii);
        }
    }
}