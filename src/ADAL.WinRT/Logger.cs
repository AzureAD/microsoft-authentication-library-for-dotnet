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
    internal class Logger : EventSource
    {
        private static readonly Logger logger;

        static Logger()
        {
            logger = new Logger();
            EnableListener();
        }

        private static void EnableListener()
        {
            if (AdalTrace.TraceEnabled)
            {
                EventListener adalListener = new StorageFileEventListener("AdalListener");
                adalListener.EnableEvents(logger, GetEventLevel(AdalTrace.Level));
            }
        }

        internal static void Verbose(CallState callState, string format, params object[] args)
        {
            logger.Verbose(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Information(CallState callState, string format, params object[] args)
        {
            logger.Information(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Warning(CallState callState, string format, params object[] args)
        {
            logger.Warning(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Error(CallState callState, string format, params object[] args)
        {
            logger.Error(LogHelper.PrepareLogMessage(callState, format, args));
        }

        [Event(1, Level = EventLevel.Verbose)]
        private void Verbose(string message)
        {
            logger.WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        private void Information(string message)
        {
            logger.WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        private void Warning(string message)
        {
            logger.WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        private void Error(string message)
        {
            logger.WriteEvent(4, message);
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