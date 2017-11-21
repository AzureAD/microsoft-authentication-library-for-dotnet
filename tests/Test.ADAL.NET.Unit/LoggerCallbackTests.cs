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
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Test.ADAL.NET.Unit
{
#pragma warning disable 0618
    internal class TestObsoleteAdalLogCallback : IAdalLogCallback
#pragma warning restore 0618
    {
        public int ErrorLogCount { get; private set; }
        public int InfoLogCount { get; private set; }
        public int WarningLogCount { get; private set; }
        public int VerboseLogCount { get; private set; }

        public int AllCallsCount { get; private set; }

        public void Log(LogLevel level, string message)
        {
            AllCallsCount++;

            switch (level)
            {
                case LogLevel.Error:
                    ErrorLogCount++;
                    break;
                case LogLevel.Warning:
                    WarningLogCount++;
                    break;
                case LogLevel.Information:
                    InfoLogCount++;
                    break;
                case LogLevel.Verbose:
                    VerboseLogCount++;
                    break;
            }
        }
    }

    [TestClass]
    public class LoggerCallbackTests
    {
        private static int _errorLogCount;
        private static int _infoLogCount;
        private static int _warningLogCount;
        private static int _verboseLogCount;

        private static int _piiErrorLogCount;
        private static int _piiInfoLogCount;
        private static int _piiWarningLogCount;
        private static int _piiVerboseLogCount;

        private LogCallback InitLogCallback()
        {
            _errorLogCount = 0;
            _infoLogCount = 0;
            _warningLogCount = 0;
            _verboseLogCount = 0;

            _piiErrorLogCount = 0;
            _piiInfoLogCount = 0;
            _piiWarningLogCount = 0;
            _piiVerboseLogCount = 0;

            return _logCallback;
        }

        private readonly LogCallback _logCallback = delegate(LogLevel level, string message, bool containsPii)
        {
            switch (level)
            {
                case LogLevel.Error:
                    if (containsPii)
                    {
                        _piiErrorLogCount += 1;
                    }
                    else
                    {
                        _errorLogCount += 1;
                    }
                    break;
                case LogLevel.Warning:
                    if (containsPii)
                    {
                        _piiWarningLogCount += 1;
                    }
                    else
                    {
                        _warningLogCount += 1;
                    }
                    break;
                case LogLevel.Information:
                    if (containsPii)
                    {
                        _piiInfoLogCount += 1;
                    }
                    else
                    {
                        _infoLogCount += 1;
                    }
                    break;
                case LogLevel.Verbose:
                    if (containsPii)
                    {
                        _piiVerboseLogCount += 1;
                    }
                    else
                    {
                        _verboseLogCount += 1;
                    }
                    break;
            }
        };

        private const string Message = "test message";

        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void ObsoleteAdalLogCallbackTest()
        {
            var logger = new Logger();
            var state = new CallState(Guid.NewGuid());

            var obsoleteCallback = new TestObsoleteAdalLogCallback();
            LoggerCallbackHandler.Callback = obsoleteCallback;

            LoggerCallbackHandler.LogCallback = null;

            LoggerCallbackHandler.PiiLoggingEnabled = true;

            logger.ErrorPii(state, new Exception(Message));
            logger.InformationPii(state, Message);
            logger.VerbosePii(state, Message);
            logger.WarningPii(state, Message);

            // make sure no Pii are logged with ObsoleteAdalLogCallback
            Assert.AreEqual(0, obsoleteCallback.AllCallsCount);

            logger.Error(state, new Exception(Message));
            Assert.AreEqual(1, obsoleteCallback.ErrorLogCount);
            Assert.AreEqual(0, obsoleteCallback.WarningLogCount);
            Assert.AreEqual(0, obsoleteCallback.InfoLogCount);
            Assert.AreEqual(0, obsoleteCallback.VerboseLogCount);

            logger.Information(state, Message);
            Assert.AreEqual(1, obsoleteCallback.ErrorLogCount);
            Assert.AreEqual(0, obsoleteCallback.WarningLogCount);
            Assert.AreEqual(1, obsoleteCallback.InfoLogCount);
            Assert.AreEqual(0, obsoleteCallback.VerboseLogCount);

            logger.Verbose(state, Message);
            Assert.AreEqual(1, obsoleteCallback.ErrorLogCount);
            Assert.AreEqual(0, obsoleteCallback.WarningLogCount);
            Assert.AreEqual(1, obsoleteCallback.InfoLogCount);
            Assert.AreEqual(1, obsoleteCallback.VerboseLogCount);

            logger.Warning(state, Message);
            Assert.AreEqual(1, obsoleteCallback.ErrorLogCount);
            Assert.AreEqual(1, obsoleteCallback.WarningLogCount);
            Assert.AreEqual(1, obsoleteCallback.InfoLogCount);
            Assert.AreEqual(1, obsoleteCallback.VerboseLogCount);
        }


        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void LogCallbackTest()
        {
            var logger = new Logger();
            var state = new CallState(Guid.NewGuid());

            var obsoleteCallback = new TestObsoleteAdalLogCallback();
            LoggerCallbackHandler.Callback = obsoleteCallback;

            LoggerCallbackHandler.LogCallback = InitLogCallback();

            LoggerCallbackHandler.PiiLoggingEnabled = true;

            logger.Error(state, new Exception(Message));
            Assert.AreEqual(1, _errorLogCount);
            Assert.AreEqual(0, _warningLogCount);
            Assert.AreEqual(0, _infoLogCount);
            Assert.AreEqual(0, _verboseLogCount);

            logger.Information(state, Message);
            Assert.AreEqual(1, _errorLogCount);
            Assert.AreEqual(0, _warningLogCount);
            Assert.AreEqual(1, _infoLogCount);
            Assert.AreEqual(0, _verboseLogCount);

            logger.Verbose(state, Message);
            Assert.AreEqual(1, _errorLogCount);
            Assert.AreEqual(0, _warningLogCount);
            Assert.AreEqual(1, _infoLogCount);
            Assert.AreEqual(1, _verboseLogCount);

            logger.Warning(state, Message);
            Assert.AreEqual(1, _errorLogCount);
            Assert.AreEqual(1, _warningLogCount);
            Assert.AreEqual(1, _infoLogCount);
            Assert.AreEqual(1, _verboseLogCount);

            // make sure no calls to Log with containsPii = true
            Assert.AreEqual(0, _piiErrorLogCount);
            Assert.AreEqual(0, _piiWarningLogCount);
            Assert.AreEqual(0, _piiInfoLogCount);
            Assert.AreEqual(0, _piiVerboseLogCount);

            // make sure no calls were done to ObsoleteAdalLogCallback
            Assert.AreEqual(0, obsoleteCallback.AllCallsCount);
        }

        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void PiiLogCallbackTest()
        {
            var logger = new Logger();
            var state = new CallState(Guid.NewGuid());

            var obsoleteCallback = new TestObsoleteAdalLogCallback();
            LoggerCallbackHandler.Callback = obsoleteCallback;

            LoggerCallbackHandler.LogCallback = InitLogCallback();

            LoggerCallbackHandler.PiiLoggingEnabled = true;

            logger.ErrorPii(state, new Exception(Message));
            Assert.AreEqual(1, _piiErrorLogCount);
            Assert.AreEqual(0, _piiWarningLogCount);
            Assert.AreEqual(0, _piiInfoLogCount);
            Assert.AreEqual(0, _piiVerboseLogCount);

            logger.InformationPii(state, Message);
            Assert.AreEqual(1, _piiErrorLogCount);
            Assert.AreEqual(0, _piiWarningLogCount);
            Assert.AreEqual(1, _piiInfoLogCount);
            Assert.AreEqual(0, _piiVerboseLogCount);

            logger.VerbosePii(state, Message);
            Assert.AreEqual(1, _piiErrorLogCount);
            Assert.AreEqual(0, _piiWarningLogCount);
            Assert.AreEqual(1, _piiInfoLogCount);
            Assert.AreEqual(1, _piiVerboseLogCount);

            logger.WarningPii(state, Message);
            Assert.AreEqual(1, _piiErrorLogCount);
            Assert.AreEqual(1, _piiWarningLogCount);
            Assert.AreEqual(1, _piiInfoLogCount);
            Assert.AreEqual(1, _piiVerboseLogCount);

            // make sure no calls to Log with containsPii = false
            Assert.AreEqual(0, _errorLogCount);
            Assert.AreEqual(0, _warningLogCount);
            Assert.AreEqual(0, _infoLogCount);
            Assert.AreEqual(0, _verboseLogCount);

            // make sure no calls were done to ObsoleteAdalLogCallback
            Assert.AreEqual(0, obsoleteCallback.AllCallsCount);
        }

        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void NullCallbackTest()
        {
            var logger = new Logger();
            var state = new CallState(Guid.NewGuid());

            LoggerCallbackHandler.Callback = null;
            LoggerCallbackHandler.LogCallback = null;

            logger.Error(state, new Exception(Message));
            logger.Information(state, Message);
            logger.Verbose(state, Message);
            logger.Warning(state, Message);

            logger.ErrorPii(state, new Exception(Message));
            logger.InformationPii(state, Message);
            logger.VerbosePii(state, Message);
            logger.WarningPii(state, Message);
        }

        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void DefaultLog_UseDefaultLoggingIsTrue_Logged()
        {
            var logger = Substitute.ForPartsOf<Logger>();

            var defaultLogCounter = 0;
            logger.When(x => x.DefaultLog(Arg.Any<LogLevel>(), Arg.Any<string>())).Do(x => defaultLogCounter++);

            var state = new CallState(Guid.NewGuid());

            LoggerCallbackHandler.PiiLoggingEnabled = true;
            LoggerCallbackHandler.UseDefaultLogging = true;

            logger.Verbose(state, Message);
            Assert.AreEqual(1, defaultLogCounter);

            logger.Information(state, Message);
            Assert.AreEqual(2, defaultLogCounter);

            logger.Warning(state, Message);
            Assert.AreEqual(3, defaultLogCounter);

            logger.Error(state, new Exception(Message));
            Assert.AreEqual(4, defaultLogCounter);
        }

        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void DefaultLog_UseDefaultLoggingIsFalse_NotLogged()
        {
            var logger = Substitute.ForPartsOf<Logger>();

            var defaultLogCounter = 0;
            logger.When(x => x.DefaultLog(Arg.Any<LogLevel>(), Arg.Any<string>())).Do(x => defaultLogCounter++);

            var state = new CallState(Guid.NewGuid());

            LoggerCallbackHandler.PiiLoggingEnabled = true;
            LoggerCallbackHandler.UseDefaultLogging = false;

            logger.Verbose(state, Message);
            Assert.AreEqual(0, defaultLogCounter);

            logger.Information(state, Message);
            Assert.AreEqual(0, defaultLogCounter);

            logger.Warning(state, Message);
            Assert.AreEqual(0, defaultLogCounter);

            logger.Error(state, new Exception(Message));
            Assert.AreEqual(0, defaultLogCounter);
        }

        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void DefaultLog_UseDefaultLoggingIsTrueContainsPii_PiiNotLogged()
        {
            var logger = Substitute.ForPartsOf<Logger>();

            var piiCounter = 0;
            logger.When(x => x.DefaultLog(Arg.Any<LogLevel>(), Arg.Any<string>())).Do(x => piiCounter++);

            var state = new CallState(Guid.NewGuid());

            LoggerCallbackHandler.PiiLoggingEnabled = true;
            LoggerCallbackHandler.UseDefaultLogging = true;

            logger.VerbosePii(state, Message);
            logger.InformationPii(state, Message);
            logger.WarningPii(state, Message);
            logger.ErrorPii(state, new Exception(Message));

            Assert.AreEqual(0, piiCounter);
        }
    }
}