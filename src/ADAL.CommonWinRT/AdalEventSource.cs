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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    [EventSource(Name = "Microsoft.IdentityModel.Clients.ActiveDirectory")]
    internal class AdalEventSource : EventSource
    {

        [Event(1, Level = EventLevel.Verbose)]
        internal void Verbose(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        internal void Information(string message)
        {
            WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        internal void Warning(string message)
        {
            WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        internal void Error(string message)
        {
            WriteEvent(4, message);
        }
    }
}
