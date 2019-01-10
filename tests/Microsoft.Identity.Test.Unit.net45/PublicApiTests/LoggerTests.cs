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
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class LoggerTests
    {
        private LogCallback _callback;

        [TestInitialize]
        public void TestInit()
        {
            TestCommon.ResetStateAndInitMsal();

            _callback = Substitute.For<LogCallback>();
            Logger.LogCallback = _callback;
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
            Logger.Level = LogLevel.Error;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Error("test message");
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.Error), Arg.Any<string>(), Arg.Is(false));

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(1, counter);
            _callback.Received().Invoke(Arg.Is(LogLevel.Error), Arg.Any<string>(), Arg.Is(false));


            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestWarning()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);
            var counter = 0;
            Logger.Level = LogLevel.Warning;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).
                Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestInfo()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            Logger.Level = LogLevel.Info;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestVerbose()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            Logger.Level = LogLevel.Verbose;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Info("test message");
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            logger.Verbose("test message");
            Assert.AreEqual(4, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestErrorPii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            Logger.Level = LogLevel.Error;
            Logger.PiiLoggingEnabled = true;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestWarningPii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            Logger.Level = LogLevel.Warning;
            Logger.PiiLoggingEnabled = true;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestInfoPii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            Logger.Level = LogLevel.Info;
            Logger.PiiLoggingEnabled = true;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerTests")]
        public void CallbackTestVerbosePii()
        {
            MsalLogger logger = new MsalLogger(Guid.Empty, null);

            var counter = 0;
            Logger.Level = LogLevel.Verbose;
            Logger.PiiLoggingEnabled = true;

            _callback.When(x => x(LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            logger.ErrorPii(new ArgumentException("test message"));
            Assert.AreEqual(1, counter);

            _callback.When(x => x(LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            logger.WarningPii("test message", string.Empty);
            Assert.AreEqual(2, counter);

            _callback.When(x => x(LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            logger.InfoPii("test message", string.Empty);
            Assert.AreEqual(3, counter);

            _callback.When(x => x(LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            logger.VerbosePii("test message", string.Empty);
            Assert.AreEqual(4, counter);
        }
    }
}
