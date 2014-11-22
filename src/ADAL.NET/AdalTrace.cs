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

using System.Diagnostics;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// The class which contains trace related properties
    /// </summary>
    public static class AdalTrace
    {
        static AdalTrace()
        {
            TraceSource = new TraceSource("Microsoft.IdentityModel.Clients.ActiveDirectory", SourceLevels.All);
            LegacyTraceSwitch = new TraceSwitch("ADALLegacySwitch", "ADAL Switch for System.Diagnostics.Trace", "Verbose");
        }

        /// <summary>
        /// Sets/gets the TraceSource that ADAL writes events to which has the name Microsoft.IdentityModel.Clients.ActiveDirectory.
        /// </summary>
        public static TraceSource TraceSource { get; private set; }

        /// <summary>
        /// Enables/disables basic tracing using class System.Diagnostics.Trace.
        /// </summary>
        public static TraceSwitch LegacyTraceSwitch { get; private set; }
    }
}
