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
using System.Diagnostics.Tracing;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal partial class Logger : IDisposable
    {
        private const string LogFilename = "AdalTraces.log";
        private bool disposed;
        private static readonly AdalEventSource AdalEventSource;
        private static StorageFileEventListener adalListener;

        static Logger()
        {
            AdalEventSource = new AdalEventSource();
        }

        internal static void SetListenerLevel(AdalTraceLevel level)
        {
            if (level != AdalTraceLevel.None)
            {
                if (adalListener == null)
                {
                    adalListener = new StorageFileEventListener(LogFilename);
                }

                adalListener.EnableEvents(AdalEventSource, GetEventLevel(level));
            }
            else if (adalListener != null)
            {
                adalListener.DisableEvents(AdalEventSource);
                adalListener.Dispose();
                adalListener = null;
            }
        }

        internal static void Error(CallState callState, Exception ex, [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "")
        {
            AdalEventSource.Error(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), "{0}", ex));
        }

        private static void Verbose(CallState callState, string callerFilePath, string format, params object[] args)
        {
            AdalEventSource.Verbose(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), format, args));
        }

        private static void Information(CallState callState, string callerFilePath, string format, params object[] args)
        {
            AdalEventSource.Information(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), format, args));
        }

        private static void Warning(CallState callState, string callerFilePath, string format, params object[] args)
        {
            AdalEventSource.Warning(PrepareLogMessage(callState, GetCallerFilename(callerFilePath), format, args));
        }

        private static EventLevel GetEventLevel(AdalTraceLevel level)
        {
            EventLevel returnLevel;
            switch (level)
            {
                case AdalTraceLevel.Informational:
                    returnLevel = EventLevel.Informational;
                    break;
                case AdalTraceLevel.Verbose:
                    returnLevel = EventLevel.Verbose;
                    break;
                case AdalTraceLevel.Warning:
                    returnLevel = EventLevel.Warning;
                    break;
                case AdalTraceLevel.Error:
                    returnLevel = EventLevel.Error;
                    break;
                case AdalTraceLevel.Critical:
                    returnLevel = EventLevel.Critical;
                    break;
                case AdalTraceLevel.LogAlways:
                    returnLevel = EventLevel.LogAlways;
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