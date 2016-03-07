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
        [TestMethod]
        [TestCategory("LoggerCallbackTests")]
        public void CallbackTest()
        {
            var counter = 0;
            Logger logger = new Logger();
            CallState state = new CallState(Guid.NewGuid());
            IMsalLogCallback callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Error, Arg.Any<string>())).Do(x=>counter++);
            LoggerCallbackHandler.Callback = callback;
            logger.Error(state, new Exception("test message"));
            Assert.AreEqual(1, counter);

            callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Information, Arg.Any<string>())).Do(x => counter++);
            LoggerCallbackHandler.Callback = callback;
            logger.Information(state, "test message");
            Assert.AreEqual(2, counter);

            callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Verbose, Arg.Any<string>())).Do(x => counter++);
            LoggerCallbackHandler.Callback = callback;
            logger.Verbose(state, "test message");
            Assert.AreEqual(3, counter);

            callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Warning, Arg.Any<string>())).Do(x => counter++);
            LoggerCallbackHandler.Callback = callback;
            logger.Warning(state, "test message");
            Assert.AreEqual(4, counter);
        }
    }
}
