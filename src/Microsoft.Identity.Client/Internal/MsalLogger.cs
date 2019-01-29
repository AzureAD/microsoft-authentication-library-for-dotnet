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
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Internal
{
    internal class MsalLogger : ICoreLogger
    {
        private readonly IPlatformLogger _platformLogger;
        private readonly LogCallback _loggingCallback;
        private readonly LogLevel _logLevel;
        private readonly bool _isDefaultPlatformLoggingEnabled;

        internal MsalLogger(Guid correlationId, string component, LogLevel logLevel, bool enablePiiLogging, bool isDefaultPlatformLoggingEnabled, LogCallback loggingCallback)
        {
            CorrelationId = correlationId;
            PiiLoggingEnabled = enablePiiLogging;
            _loggingCallback = loggingCallback;
            _logLevel = logLevel;
            _isDefaultPlatformLoggingEnabled = isDefaultPlatformLoggingEnabled;

            _platformLogger = PlatformProxyFactory.CreatePlatformProxy(null).PlatformLogger;
            Component = string.Empty;
            if (!string.IsNullOrEmpty(component))
            {
                //space is intentional for formatting of the message
                Component = string.Format(CultureInfo.InvariantCulture, " ({0})", component);
            }
        }

        public static ICoreLogger Create(Guid correlationId, IApplicationConfiguration config, bool isDefaultPlatformLoggingEnabled = false)
        {
            return new MsalLogger(
                correlationId,
                config?.Component ?? string.Empty,
                config?.LogLevel ?? LogLevel.Verbose,
                config?.EnablePiiLogging ?? false,
                config?.IsDefaultPlatformLoggingEnabled ?? isDefaultPlatformLoggingEnabled,
                config?.LoggingCallback ?? null);
        }

        public static ICoreLogger CreateNullLogger()
        {
            return new NullLogger();
        }

        public Guid CorrelationId { get; }

        public bool PiiLoggingEnabled { get; }

        internal string Component { get; }

        public void Info(string messageScrubbed)
        {
            Log(LogLevel.Info, string.Empty, messageScrubbed);
        }

        public void InfoPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Info, messageWithPii, messageScrubbed);
        }

        public void InfoPii(Exception exWithPii)
        {
            Log(LogLevel.Info, exWithPii.ToString(), MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Info, prefix + exWithPii.ToString(), prefix + MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public void Verbose(string messageScrubbed)
        {
            Log(LogLevel.Verbose, string.Empty, messageScrubbed);
        }

        public void VerbosePii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Verbose, messageWithPii, messageScrubbed);
        }

        public void Warning(string messageScrubbed)
        {
            Log(LogLevel.Warning, string.Empty, messageScrubbed);
        }

        public void WarningPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Warning, messageWithPii, messageScrubbed);
        }

        public void WarningPii(Exception exWithPii)
        {
            Log(LogLevel.Warning, exWithPii.ToString(), MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Warning, prefix + exWithPii.ToString(), prefix + MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public void Error(string messageScrubbed)
        {
            Log(LogLevel.Error, string.Empty, messageScrubbed);
        }

        public void ErrorPii(Exception exWithPii)
        {
            Log(LogLevel.Error, exWithPii.ToString(), MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Log(LogLevel.Error, prefix + exWithPii.ToString(), prefix + MsalExceptionFactory.GetPiiScrubbedExceptionDetails(exWithPii));
        }

        public void ErrorPii(string messageWithPii, string messageScrubbed)
        {
            Log(LogLevel.Error, messageWithPii, messageScrubbed);
        }

        private void Log(LogLevel msalLogLevel, string messageWithPii, string messageScrubbed)
        {
            if (_loggingCallback == null || msalLogLevel > _logLevel)
            {
                return;
            }

            //format log message;
            string correlationId = CorrelationId.Equals(Guid.Empty)
                ? string.Empty
                : " - " + CorrelationId;

            var msalIdParameters = MsalIdHelper.GetMsalIdParameters(this);
            string os = "N/A";
            if (msalIdParameters.TryGetValue(MsalIdParameter.OS, out string osValue))
            {
                os = osValue;
            }

            bool messageWithPiiExists = !string.IsNullOrWhiteSpace(messageWithPii);
            // If we have a message with PII, and PII logging is enabled, use the PII message, else use the scrubbed message.
            bool isLoggingPii = messageWithPiiExists && PiiLoggingEnabled;
            string messageToLog = isLoggingPii ? messageWithPii : messageScrubbed;

            string log = string.Format(CultureInfo.InvariantCulture, "{0} MSAL {1} {2} {3} [{4}{5}]{6} {7}",
                isLoggingPii ? "(True)" : "(False)",
                MsalIdHelper.GetMsalVersion(),
                msalIdParameters[MsalIdParameter.Product],
                os, DateTime.UtcNow, correlationId, Component, messageToLog);

            if (_isDefaultPlatformLoggingEnabled)
            {
                switch (msalLogLevel)
                {
                    case LogLevel.Error:
                        _platformLogger.Error(log);
                        break;
                    case LogLevel.Warning:
                        _platformLogger.Warning(log);
                        break;
                    case LogLevel.Info:
                        _platformLogger.Information(log);
                        break;
                    case LogLevel.Verbose:
                        _platformLogger.Verbose(log);
                        break;
                }
            }

            _loggingCallback.Invoke(msalLogLevel, log, isLoggingPii);
        }
    }
}
