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

using System.Diagnostics.Tracing;
using System.Globalization;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class Logger : EventSource
    {
        private static readonly Logger logger = CreateInstance();

        private static Logger CreateInstance()
        {
            Logger logger = new Logger();
            EventListener verboseListener = new StorageFileEventListener("MyListenerVerbose");
            verboseListener.EnableEvents(logger, EventLevel.Verbose);
            return logger;
        }

        internal static void Verbose(CallState callState, string format, params object[] args)
        {
            Verbose(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Information(CallState callState, string format, params object[] args)
        {
            Information(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Warning(CallState callState, string format, params object[] args)
        {
            Warning(LogHelper.PrepareLogMessage(callState, format, args));
        }

        internal static void Error(CallState callState, string format, params object[] args)
        {
            Error(LogHelper.PrepareLogMessage(callState, format, args));
        }

        [Event(1, Level = EventLevel.Verbose)]
        private static void Verbose(string message)
        {
            logger.WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        private static void Information(string message)
        {
            logger.WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        private static void Warning(string message)
        {
            logger.WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        private static void Error(string message)
        {
            logger.WriteEvent(4, message);
        }

    }
}