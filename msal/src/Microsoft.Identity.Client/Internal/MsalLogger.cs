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
using Microsoft.Identity.Core;

namespace Microsoft.Identity.Client.Internal
{
    internal class MsalLogger : CoreLoggerBase
    {
        internal MsalLogger(Guid correlationId, string component) : base(correlationId)
        {
            Component = string.Empty;
            if (!string.IsNullOrEmpty(component))
            {
                //space is intentional for formatting of the message
                Component = string.Format(CultureInfo.InvariantCulture, " ({0})", component);
            }
        }

        public override bool PiiLoggingEnabled => Logger.PiiLoggingEnabled;

        internal string Component { get; set; }

        public override void Info(string messageScrubbed)
        {
            Log(LogLevel.Info, string.Empty, messageScrubbed);
        }

        public override void InfoPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Info, messageWithPii, messageScrubbed);
        }

        public override void InfoPii(Exception exWithPii)
        {
            Log(LogLevel.Info, exWithPii.ToString(), MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Info, prefix + exWithPii.ToString(), prefix + MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void Verbose(string messageScrubbed)
        {
            Log(LogLevel.Verbose, string.Empty, messageScrubbed);
        }

        public override void VerbosePii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Verbose, messageWithPii, messageScrubbed);
        }

        public override void Warning(string messageScrubbed)
        {
            Log(LogLevel.Warning, string.Empty, messageScrubbed);
        }

        public override void WarningPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Warning, messageWithPii, messageScrubbed);
        }

        public override void WarningPii(Exception exWithPii)
        {
            Log(LogLevel.Warning, exWithPii.ToString(), MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Warning, prefix + exWithPii.ToString(), prefix + MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void Error(string messageScrubbed)
        {
            Log(LogLevel.Error, string.Empty, messageScrubbed);
        }

        public override void ErrorPii(Exception exWithPii)
        {
            Log(LogLevel.Error, exWithPii.ToString(), MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Error, prefix + exWithPii.ToString(), prefix + MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void ErrorPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Error, messageWithPii, messageScrubbed);
        }

        private static void ExecuteCallback(LogLevel level, string message, bool containsPii)
        {
            lock (Logger.LockObj)
            {
                Logger.LogCallback?.Invoke(level, message, containsPii);
            }
        }

        private void Log(LogLevel msalLogLevel, string messageWithPii, string messageScrubbed)
        {
            if (msalLogLevel > Logger.Level)
            {
                return;
            }

            //format log message;
            string correlationId = (CorrelationId.Equals(Guid.Empty))
                ? string.Empty
                : " - " + CorrelationId;

            var msalIdParameters = MsalIdHelper.GetMsalIdParameters(new PlatformInformation());
            string os = "N/A";
            if (msalIdParameters.ContainsKey(MsalIdParameter.OS))
            {
                os = msalIdParameters[MsalIdParameter.OS];
            }

            bool messageWithPiiExists = !string.IsNullOrWhiteSpace(messageWithPii);
            // If we have a message with PII, and PII logging is enabled, use the PII message, else use the scrubbed message.
            bool isLoggingPii = messageWithPiiExists && Logger.PiiLoggingEnabled;
            string messageToLog = isLoggingPii ? messageWithPii : messageScrubbed;

            string log = string.Format(CultureInfo.InvariantCulture, "{0} MSAL {1} {2} {3} [{4}{5}]{6} {7}",
                isLoggingPii ? "(True)" : "(False)",
                MsalIdHelper.GetMsalVersion(),
                msalIdParameters[MsalIdParameter.Product],
                os, DateTime.UtcNow, correlationId, Component, messageToLog);

            if (Logger.DefaultLoggingEnabled)
            {
                switch (Logger.Level)
                {
                    case LogLevel.Error:
                        PlatformLogger.Error(log);
                        break;
                    case LogLevel.Warning:
                        PlatformLogger.Warning(log);
                        break;
                    case LogLevel.Info:
                        PlatformLogger.Information(log);
                        break;
                    case LogLevel.Verbose:
                        PlatformLogger.Verbose(log);
                        break;
                }
            }

            ExecuteCallback(msalLogLevel, log, isLoggingPii);
        }
    }
}
