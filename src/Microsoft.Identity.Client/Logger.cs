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
    public class Logger
    {
        private RequestContext requestContext;

        public enum LogLevel
        {
            Error = 0,
            Warning = 1,
            Info = 2,
            Verbose = 3
        }

        internal Logger(Guid correlationId)
        {
            this.CorrelationId = correlationId;
        }

        public Logger()
        {
        }

        internal Guid CorrelationId { get; set; }

        /// <summary>
        /// The default log level is set to info.
        /// </summary>
        internal LogLevel ApplicationLogLevel { get; set; } = LogLevel.Info;

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
            if (logLevel > ApplicationLogLevel) return;

            //format log message;
            string correlationId = (CorrelationId.Equals(Guid.Empty))
                ? "No CorrelationId"
                : CorrelationId.ToString();

            string log = string.Format(CultureInfo.InvariantCulture, "MSAL {0} {1} {2} [{3} {4}] {5}", MsalIdHelper.GetMsalVersion(),
                PlatformPlugin.PlatformInformation.GetProductName(),
                PlatformPlugin.PlatformInformation.GetAssemblyFileVersionAttribute(), DateTime.UtcNow,
                correlationId, logMessage);

            //platformPlugin
            if (!containsPii)
            {
                PlatformPlugin.LogMessage(logLevel, log);
            }

            //callback();
            LoggerCallbackHandler.ExecuteCallback(logLevel, log, containsPii);
        }
    }
}