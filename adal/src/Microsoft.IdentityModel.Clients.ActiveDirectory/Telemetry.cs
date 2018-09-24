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

using Microsoft.Identity.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Facilitates the acquisition of telemtry data
    /// </summary>
    /// <remarks>
    /// This blank class used as a place holder for CoreTelemetryService.Instance to avaoid null reference exceptions in core. No implementation is present at this time.
    /// </remarks>
    internal class Telemetry : ITelemetry
    {
        private static readonly ITelemetry Instance = new Telemetry();

        public static Telemetry GetInstance()
        {
            return Instance as Telemetry;
        }

        /// <summary>
        /// Starts Telemetry event
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="eventToStart"></param>
        public void StartEvent(string requestId, EventBase eventToStart)
        {
            
        }

        /// <summary>
        /// Stops Telemetry event
        /// </summary>
        /// <param name="requestId"></param>
        /// <param name="eventToStop"></param>
        public void StopEvent(string requestId, EventBase eventToStop)
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestId"></param>
        public void Flush(string requestId)
        {

        }
    }
}
