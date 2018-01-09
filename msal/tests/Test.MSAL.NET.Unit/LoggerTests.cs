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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class LoggerTests
    {
        //private static ILoggerCallback _callback;
        private static LogCallback _callback;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _callback = Substitute.For<LogCallback>();
            MsalLoggerSettings.LogCallback = _callback;
        }
        
        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void ConstructorComponentTest()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);
            Assert.AreEqual(string.Empty, logger.Component);
            logger = new MsalLogger(Guid.Empty, "comp1");
            Assert.AreEqual(" (comp1)", logger.Component);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestErrorTest()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);
            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Error;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error("test message");
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestWarning()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);
            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Warning;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestInfo()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Info;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestVerbose()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Verbose;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(4, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestErrorPii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Error;
            MsalLoggerSettings.PiiLoggingEnabled = true;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message");
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message");
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message");
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestWarningPii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Warning;
            MsalLoggerSettings.PiiLoggingEnabled = true;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message");
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestInfoPii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Info;
            MsalLoggerSettings.PiiLoggingEnabled = true;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message");
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestVerbosePii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            MsalLoggerSettings.Level = MsalLogLevel.Verbose;
            MsalLoggerSettings.PiiLoggingEnabled = true;

            _callback.When(x => x(MsalLogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(MsalLogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(MsalLogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(MsalLogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message");
            Assert.AreEqual(4, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void ScrubPiiExceptionsTest()
        {
            Exception ex = new Exception("test message");
            var result = ex.GetPiiScrubbedDetails();
            Assert.AreEqual("Exception type: System.Exception", result);

            result = ((Exception) null).GetPiiScrubbedDetails();
            Assert.AreEqual(null, result);

            Exception innerException = new Exception("test message", new Exception("inner message"));
            result = innerException.GetPiiScrubbedDetails();
            Assert.AreEqual("Exception type: System.Exception---> Exception type: System.Exception\r\n=== End of inner exception stack trace ===",
                result);

            MsalException msalException = new MsalException("Msal Exception");
            result = msalException.GetPiiScrubbedDetails();
            Assert.AreEqual("Exception type: Microsoft.Identity.Client.MsalException, ErrorCode: Msal Exception", result);

            MsalServiceException msalServiceException = new MsalServiceException("ErrorCode", "Msal Service Exception");
            result = msalServiceException.GetPiiScrubbedDetails();
            Assert.AreEqual("Exception type: Microsoft.Identity.Client.MsalServiceException, ErrorCode: ErrorCode, StatusCode: 0", result);
        }
    }
}
