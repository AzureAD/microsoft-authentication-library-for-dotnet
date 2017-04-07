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

namespace Microsoft.Identity.Client
{
    internal class Event : Dictionary<string, string>
    {
        private readonly long _startTimestamp;

        public Event(string eventName) : this(eventName, new Dictionary<string, string>()) {}

        protected static long ToUnixTimeMilliseconds()  // Backport .NET 4.6 DateTimeOffset.Now.ToUnixTimeSeconds()
        {
            var dateTime = DateTime.Now;
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(dateTime.ToUniversalTime() - epoch).TotalMilliseconds;
        }

        public Event(string eventName, IDictionary<string, string> predefined) : base(predefined)
        {
            this["event_name"] = eventName;
            _startTimestamp = ToUnixTimeMilliseconds();
            this["start_time"] = _startTimestamp.ToString();
            this["stop_time"] = "-1";
        }

        public void Stop()
        {
            var stopTimestamp = ToUnixTimeMilliseconds();
            this["stop_time"] = stopTimestamp.ToString();  // It is a timestamp
            this["elapsed_time"] = (stopTimestamp - _startTimestamp).ToString();  // It is a duration
        }
    }
}
