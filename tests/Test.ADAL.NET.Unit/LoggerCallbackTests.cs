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
