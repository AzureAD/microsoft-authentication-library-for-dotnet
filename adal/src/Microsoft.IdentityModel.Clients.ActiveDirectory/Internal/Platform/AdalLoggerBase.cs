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
            PiiLoggingEnabled = LoggerCallbackHandler.PiiLoggingEnabled;
        }

        private void Log(LogLevel logLevel, string message, bool containsPii,
            [CallerFilePath] string callerFilePath = "")
        {
            if (!LoggerCallbackHandler.PiiLoggingEnabled && containsPii)
            {
                return;
            }

            var formattedMessage = FormatLogMessage(GetCallerFilename(callerFilePath), message);

            if (LoggerCallbackHandler.UseDefaultLogging && !containsPii)
            {
                DefaultLog(logLevel, formattedMessage);
            }

            if (LoggerCallbackHandler.LogCallback != null)
            {
                LoggerCallbackHandler.ExecuteCallback(logLevel, formattedMessage, containsPii);
            }
            else if (!containsPii)
            {
                // execute obsolete IAdalLogCallback only if LogCallback is not set and message does not contain Pii
                LoggerCallbackHandler.ExecuteObsoleteCallback(logLevel, formattedMessage);
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

        public override void InfoPii(string message)
        {
            Log(LogLevel.Information, message, true);
        }

        public override void Verbose(string message)
        {
            Log(LogLevel.Verbose, message, false);
        }

        public override void VerbosePii(string message)
        {
            Log(LogLevel.Verbose, message, true);
        }

        public override void ErrorPii(string message)
        {
            Log(LogLevel.Error, message, true);
        }

        public override void Warning(string message)
        {
            Log(LogLevel.Warning, message, false);
        }

        public override void WarningPii(string message)
        {
            Log(LogLevel.Warning, message, true);
        }

        public override void Info(string message)
        {
            Log(LogLevel.Information, message, false);
        }

        public override void Error(Exception ex)
        {
            Log(LogLevel.Error, AdalExceptionFactory.GetPiiScrubbedExceptionDetails(ex), false);
        }

        public override void ErrorPii(Exception ex)
        {
            ErrorPii(ex.ToString());
        }

        public override void Error(string message)
        {
            Log(LogLevel.Error, message, false);
        }
    }
}
