//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using Windows.Foundation.Diagnostics;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class Logger : IDisposable
    {
        private const string LogFilename = "AdalTraces.log";
        private bool disposed;
        private static readonly LoggingChannel AdalEventSource;
        private static FileLoggingSession adalListener;

        static Logger()
        {
            AdalEventSource = new LoggingChannel("Microsoft.IdentityModel.Clients.ActiveDirectory");
        }

        internal static void SetListenerLevel(AdalTraceLevel level)
        {
            if (level != AdalTraceLevel.None)
            {
                if (adalListener == null)
                {
                    adalListener = new FileLoggingSession(LogFilename);
                }

                adalListener.AddLoggingChannel(AdalEventSource, GetEventLevel(level));
            }
            else if (adalListener != null)
            {
                adalListener.RemoveLoggingChannel(AdalEventSource);
                adalListener.Dispose();
                adalListener = null;
            }
        }

        internal static void Error(CallState callState, Exception ex, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            AdalEventSource.LogMessage(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), "{0}", ex), LoggingLevel.Error);
        }

        private static void Verbose(CallState callState, string format, string callerFilePath, params object[] args)
        {
            AdalEventSource.LogMessage(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), format, args), LoggingLevel.Verbose);
        }

        private static void Information(CallState callState, string callerFilePath, string format, params object[] args)
        {
            AdalEventSource.LogMessage(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), format, args), LoggingLevel.Information);
        }

        private static void Warning(CallState callState, string callerFilePath, string format, params object[] args)
        {
            AdalEventSource.LogMessage(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), format, args), LoggingLevel.Warning);
        }

        private static LoggingLevel GetEventLevel(AdalTraceLevel level)
        {
            LoggingLevel returnLevel;
            switch (level)
            {
                case AdalTraceLevel.Informational:
                    returnLevel = LoggingLevel.Information;
                    break;
                case AdalTraceLevel.Verbose:
                    returnLevel = LoggingLevel.Verbose;
                    break;
                case AdalTraceLevel.Warning:
                    returnLevel = LoggingLevel.Warning;
                    break;
                case AdalTraceLevel.Error:
                    returnLevel = LoggingLevel.Error;
                    break;
                case AdalTraceLevel.Critical:
                    returnLevel = LoggingLevel.Critical;
                    break;
                case AdalTraceLevel.LogAlways:
                    returnLevel = LoggingLevel.Verbose;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
            return returnLevel;
        }

        private static string GetCallerFilename(string callerFilePath)
        {
            return callerFilePath.Substring(callerFilePath.LastIndexOf("\\", StringComparison.Ordinal) + 1);            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (adalListener != null)
                    {
                        try
                        {
                            adalListener.Dispose();
                        }
                        catch
                        {
                            // ignore
                        }

                        adalListener = null;
                    }

                    if (AdalEventSource != null)
                    {
                        try
                        {
                            AdalEventSource.Dispose();
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }

                disposed = true;
            }
        }
    }
}