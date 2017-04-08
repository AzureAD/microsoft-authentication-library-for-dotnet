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

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class Event : Dictionary<string, string>
    {
        protected const string EventName = "event_name";
        protected const string StartTime = "start_time";
        protected const string StopTime = "stop_time";
        protected const string ElapsedTime = "elapsed_time";
        private readonly long _startTimestamp;

        public Event(string eventName) : this(eventName, new Dictionary<string, string>()) {}

        protected static long CurrentUnixTimeMilliseconds()
        {
            return MsalHelpers.DateTimeToUnixTimestampMilliseconds(DateTimeOffset.Now);
        }

        public Event(string eventName, IDictionary<string, string> predefined) : base(predefined)
        {
            this[EventName] = eventName;
            _startTimestamp = CurrentUnixTimeMilliseconds();
            this[StartTime] = _startTimestamp.ToString();
            this[StopTime] = "-1";
        }

        public void Stop()
        {
            var stopTimestamp = CurrentUnixTimeMilliseconds();
            this[StopTime] = stopTimestamp.ToString();  // It is a timestamp
            this[ElapsedTime] = (stopTimestamp - _startTimestamp).ToString();  // It is a duration
        }
    }
}
