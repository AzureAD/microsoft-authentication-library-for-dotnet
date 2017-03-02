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
using static Microsoft.Identity.Client.MsalLogger;

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
            IMsalLogCallback callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Error, Arg.Any<string>())).Do(x=>counter++);
            LoggerCallbackHandler.Callback = callback;
            MsalLogger.Error(new Exception("test message"));
            Assert.AreEqual(1, counter);

            callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Info, Arg.Any<string>())).Do(x => counter++);
            LoggerCallbackHandler.Callback = callback;
            MsalLogger.Info("test message");
            Assert.AreEqual(2, counter);

            callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Verbose, Arg.Any<string>())).Do(x => counter++);
            LoggerCallbackHandler.Callback = callback;
            MsalLogger.Verbose( "test message");
            Assert.AreEqual(3, counter);

            callback = Substitute.For<IMsalLogCallback>();
            callback.When(x => x.Log(LogLevel.Warning, Arg.Any<string>())).Do(x => counter++);
            LoggerCallbackHandler.Callback = callback;
            MsalLogger.Warning("test message");
            Assert.AreEqual(4, counter);
        }
    }
}