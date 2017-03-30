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
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    class MyReceiver
    {
        public void OnEvents(List<Dictionary<string, string>> events)
        {
            foreach(var e in events)
            {
                foreach(var entry in e)
                {
                    Console.WriteLine("{0}: {1}", entry.Key, entry.Value);
                }
            }
        }
    }

    [TestClass]
    public class TelemetryTests
    {
        [TestInitialize]
        public void Initialize()
        {
        }

        [TestMethod]
        [TestCategory("TelemetryTests")]
        public void TelemetryPublicApiSample()
        {
            var telemetry = Telemetry.GetInstance();
            var receiver = new MyReceiver();
            telemetry.RegisterReceiver(receiver.OnEvents);

            // Or you can use a one-liner:
            Telemetry.GetInstance().RegisterReceiver(new MyReceiver().OnEvents);
        }

        [TestMethod]
        [TestCategory("TelemetryTests")]
        public void TelemetryIsSingleton()
        {
            var t1 = Telemetry.GetInstance();
            Assert.IsNotNull(t1);
            var t2 = Telemetry.GetInstance();
            Assert.AreEqual(t1, t2);
        }
    }
}
