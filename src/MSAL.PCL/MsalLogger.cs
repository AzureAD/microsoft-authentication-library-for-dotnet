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
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    public class MsalLogger
    {
        public enum LogLevel
        {
            Error = 0,
            Warning = 1,
            Info = 2,
            Verbose = 3
        }

        internal MsalLogger(Guid correlationId)
        {
            this.CorrelationId = correlationId;
        }

        internal Guid CorrelationId { get; set; }

        internal LogLevel ApplicationLogLevel { get; set; } = LogLevel.Info;

        #region LogMessages

        public void Error(string message)
        {
            LogMessage(message, LogLevel.Error);
        }
        public void Warning(string message)
        {
            LogMessage(message, LogLevel.Warning);
        }
        public void Info(string message)
        {
            LogMessage(message, LogLevel.Info);
        }
        public void Verbose(string message)
        {
            LogMessage(message, LogLevel.Verbose);
        }

        public void Error(Exception ex)
        {
            Error(ex.ToString());
        }
        public void Warning(Exception ex)
        {
            Warning(ex.ToString());
        }
        public void Info(Exception ex)
        {
            Info(ex.ToString());
        }
        public void Verbose(Exception ex)
        {
            Verbose(ex.ToString());
        }

        #endregion

        private void LogMessage(string logMessage, LogLevel logLevel)
        {
            if (logLevel > ApplicationLogLevel) return;

            //format log message;
            string correlationId = (CorrelationId.Equals(Guid.Empty))
                ? "No CorrelationId"
                : CorrelationId.ToString();
            string log = string.Format(CultureInfo.CurrentCulture, "{0}: {1}: {2}", DateTime.UtcNow, correlationId,
                logMessage);

            //platformPlugin
            PlatformPlugin.LogMessage(logLevel, log);

            //callback();
            LoggerCallbackHandler.ExecuteCallback(logLevel, log);
        }
    }
}