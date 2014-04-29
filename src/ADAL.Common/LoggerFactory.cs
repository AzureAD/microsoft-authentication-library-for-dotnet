
using System.Diagnostics.Tracing;
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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Common
{
    internal sealed class LoggerFactory
    {
        private LoggerFactory(){}
        private static ILogger logger;
        private static readonly object objLock = new object();

        internal static ILogger getLogger()
        {
            lock (objLock)
            {
                if (logger == null)
                {
#if ADAL_WINRT
                    WinRTLogger myLogger = new WinRTLogger();
                    // First time execution, initialize the logger 
                    EventListener verboseListener = new StorageFileEventListener("MyListenerVerbose");
                    verboseListener.EnableEvents(myLogger, EventLevel.Verbose);
                    logger = myLogger;
#else
                    logger = new TraceLogger();
#endif
                }
                return logger;
            }
        }
    }
}
