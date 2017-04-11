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
    public class LoggerCallbackTests
    {
        //private static ILoggerCallback _callback;
        private static LogDelegate _delegate;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            //_callback = Substitute.For<ILoggerCallback>();
            //Logger.Callback = _callback;
            _delegate = Substitute.For<LogDelegate>();
            Logger.LogDelegate = _delegate;
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestError()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Error;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Warning("test message");
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Info("test message");
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Verbose("test message");
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestWarning()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Warning;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Info("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Verbose("test message");
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestInfo()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Info;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Info("test message");
            Assert.AreEqual(3, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Verbose("test message");
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestVerbose()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Verbose;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Warning("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Info("test message");
            Assert.AreEqual(3, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), false)).Do(x => counter++);
            requestContext.Logger.Verbose("test message");
            Assert.AreEqual(4, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestErrorPii()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Error;
            Logger.PiiLoggingEnabled = true;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.WarningPii("test message");
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.InfoPii("test message");
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.VerbosePii("test message");
            Assert.AreEqual(1, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestWarningPii()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Warning;
            Logger.PiiLoggingEnabled = true;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.WarningPii("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.InfoPii("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.VerbosePii("test message");
            Assert.AreEqual(2, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestInfoPii()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Info;
            Logger.PiiLoggingEnabled = true;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.WarningPii("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.InfoPii("test message");
            Assert.AreEqual(3, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.VerbosePii("test message");
            Assert.AreEqual(3, counter);
        }

        [TestMethod()]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTestVerbosePii()
        {
            RequestContext requestContext = new RequestContext(Guid.Empty);

            var counter = 0;
            Logger.Level = Logger.LogLevel.Verbose;
            Logger.PiiLoggingEnabled = true;

            _delegate.When(x => x(Logger.LogLevel.Error, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.ErrorPii(new Exception("test message"));
            Assert.AreEqual(1, counter);

            _delegate.When(x => x(Logger.LogLevel.Warning, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.WarningPii("test message");
            Assert.AreEqual(2, counter);

            _delegate.When(x => x(Logger.LogLevel.Info, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.InfoPii("test message");
            Assert.AreEqual(3, counter);

            _delegate.When(x => x(Logger.LogLevel.Verbose, Arg.Any<string>(), true)).Do(x => counter++);
            requestContext.Logger.VerbosePii("test message");
            Assert.AreEqual(4, counter);
        }
    }
}
