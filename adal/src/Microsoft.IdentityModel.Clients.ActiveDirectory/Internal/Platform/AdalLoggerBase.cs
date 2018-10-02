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
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Identity.Core;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform
{
    internal abstract class AdalLoggerBase : CoreLoggerBase
    {
        internal abstract void DefaultLog(LogLevel logLevel, string message);

        static AdalLoggerBase()
        {
            Default = new AdalLogger(Guid.Empty);
        }

        protected AdalLoggerBase(Guid correlationId) :base(correlationId)
        {
        }

        public override bool PiiLoggingEnabled => LoggerCallbackHandler.PiiLoggingEnabled;

        private void Log(LogLevel logLevel, string messageWithPii, string messageScrubbed,
            [CallerFilePath] string callerFilePath = "")
        {
            bool messageWithPiiExists = !string.IsNullOrWhiteSpace(messageWithPii);
            bool messageScrubbedExists = !string.IsNullOrWhiteSpace(messageScrubbed);

            // don't log PII to DefaultLog
            if (LoggerCallbackHandler.UseDefaultLogging && messageScrubbedExists)
            {
                DefaultLog(logLevel, FormatLogMessage(GetCallerFilename(callerFilePath), messageScrubbed));
            }

            if (LoggerCallbackHandler.LogCallback != null)
            {
                // If we have a message with PII, and PII logging is enabled, use the PII message, else use the scrubbed message.
                if (messageWithPiiExists && LoggerCallbackHandler.PiiLoggingEnabled)
                {
                    LoggerCallbackHandler.ExecuteCallback(
                        logLevel, 
                        FormatLogMessage(GetCallerFilename(callerFilePath), messageWithPii), 
                        true);
                }
                else if (messageScrubbedExists)
                {
                    LoggerCallbackHandler.ExecuteCallback(
                        logLevel,
                        FormatLogMessage(GetCallerFilename(callerFilePath), messageScrubbed),
                        false);
                }
            }
            else if (messageScrubbedExists)
            {
                // execute obsolete IAdalLogCallback only if LogCallback is not set and message does not contain Pii
                LoggerCallbackHandler.ExecuteObsoleteCallback(logLevel, FormatLogMessage(GetCallerFilename(callerFilePath), messageScrubbed));
            }
        }

        internal static string GetCallerFilename(string callerFilePath)
        {
            return callerFilePath.Substring(callerFilePath.LastIndexOf("\\", StringComparison.Ordinal) + 1);
        }

        internal string FormatLogMessage(string classOrComponent, string message)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:O}: {1} - {2}: {3}", DateTime.UtcNow, CorrelationId, classOrComponent, message);
        }

        public override void InfoPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Information, messageWithPii, messageScrubbed);
        }

        public override void InfoPii(Exception exWithPii)
        {
            Log(LogLevel.Information, exWithPii.ToString(), AdalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Information, prefix + exWithPii.ToString(), prefix + AdalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
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
            Log(LogLevel.Warning, exWithPii.ToString(), AdalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Warning, prefix + exWithPii.ToString(), prefix + AdalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void Info(string messageScrubbed)
        {
            Log(LogLevel.Information, string.Empty, messageScrubbed);
        }

        public override void ErrorPii(Exception exWithPii)
        {
            Log(LogLevel.Error, exWithPii.ToString(), AdalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Error, prefix + exWithPii.ToString(), prefix + AdalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public override void Error(string messageWithPii)
        {
            Log(LogLevel.Error, messageWithPii, string.Empty);
        }

        public override void ErrorPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Error, messageWithPii, string.Empty);
        }
    }
}
