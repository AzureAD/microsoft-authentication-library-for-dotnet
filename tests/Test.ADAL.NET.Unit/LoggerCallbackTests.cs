//------------------------------------------------------------------------------
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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.ADAL.NET.Unit
{
    class TestCallback : IAdalLogCallback
    {
        public int ErrorLogCount { get; private set; }
        public int InfoLogCount { get; private set; }
        public int WarningLogCount { get; private set; }
        public int VerboseLogCount { get; private set; }

        public void Log(LogLevel level, string message)
        {
            if (level == LogLevel.Error)
            {
                ErrorLogCount += 1;
            }

            if (level == LogLevel.Warning)
            {
                WarningLogCount += 1;
            }

            if (level == LogLevel.Information)
            {
                InfoLogCount += 1;
            }

            if (level == LogLevel.Verbose)
            {
                VerboseLogCount += 1;
            }
        }
    }

    [TestClass]
    public class LoggerCallbackTests
    {
        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTest()
        {
            Logger logger = new Logger();
            CallState state = new CallState(Guid.NewGuid());
            TestCallback callback = new TestCallback();
            LoggerCallbackHandler.Callback = callback;

            logger.Error(state, new Exception("test message"));
            Assert.AreEqual(1, callback.ErrorLogCount);
            Assert.AreEqual(0, callback.WarningLogCount);
            Assert.AreEqual(0, callback.InfoLogCount);
            Assert.AreEqual(0, callback.VerboseLogCount);

            logger.Information(state, "test message");
            Assert.AreEqual(1, callback.ErrorLogCount);
            Assert.AreEqual(0, callback.WarningLogCount);
            Assert.AreEqual(1, callback.InfoLogCount);
            Assert.AreEqual(0, callback.VerboseLogCount);

            logger.Verbose(state, "test message");
            Assert.AreEqual(1, callback.ErrorLogCount);
            Assert.AreEqual(0, callback.WarningLogCount);
            Assert.AreEqual(1, callback.InfoLogCount);
            Assert.AreEqual(1, callback.VerboseLogCount);

            logger.Warning(state, "test message");
            Assert.AreEqual(1, callback.ErrorLogCount);
            Assert.AreEqual(1, callback.WarningLogCount);
            Assert.AreEqual(1, callback.InfoLogCount);
            Assert.AreEqual(1, callback.VerboseLogCount);
        }

        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void NullCallbackTest()
        {
            Logger logger = new Logger();
            CallState state = new CallState(Guid.NewGuid());
            logger.Error(state, new Exception("test message"));
            logger.Information(state, "test message");
            logger.Verbose(state, "test message");
            logger.Warning(state, "test message");
        }
    }
}
