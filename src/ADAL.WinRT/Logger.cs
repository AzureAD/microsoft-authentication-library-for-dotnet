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
    internal class Logger
    {
        private static readonly AdalEventSource adalEventSource;
        private static EventListener adalListener;

        static Logger()
        {
            adalEventSource = new AdalEventSource();
        }

        internal static void SetListenerLevel(AdalTraceLevel level)
        {
            if (level != AdalTraceLevel.None)
            {
                if (adalListener == null)
                {
                    adalListener = new StorageFileEventListener("AdalListener");
                }
                adalListener.EnableEvents(adalEventSource, GetEventLevel(level));
            }
            else if (adalListener != null)
            {
                adalListener.DisableEvents(adalEventSource);
                adalListener.Dispose();
                adalListener = null;
            }
        }

        internal static void Verbose(CallState callState, string format, params object[] args)
        {
            adalEventSource.Verbose(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Information(CallState callState, string format, params object[] args)
        {
            adalEventSource.Information(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Warning(CallState callState, string format, params object[] args)
        {
            adalEventSource.Warning(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Error(CallState callState, string format, params object[] args)
        {
            adalEventSource.Error(LogHelper.PrepareLogMessage(callState, format, args));
        }

        private static EventLevel GetEventLevel(AdalTraceLevel level)
        {
            EventLevel returnLevel = EventLevel.Informational;
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
    }
}