// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.Telemetry
{
    [TestClass]
    public class XmsCliTelemTests
    {
        private RequestContext _requestContext;
        private ICoreLogger _coreLogger;

        [TestInitialize]
        public void TestInitialize()
        {
            // Methods in XmsCliTelemTests log errors when parsing response headers;
            _coreLogger = Substitute.For<ICoreLogger>();
            _requestContext = new RequestContext(null, _coreLogger);
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void XmsClientTelemInfoParseTest_XmsCliTelemInfoCorrectFormat()
        {
            //Act - Parse correctly formatted header
            var responseHeaders = new Dictionary<string, string>
            {
                {"x-ms-clitelem", "1,0,0,,"}
            };

            var xmsCliTeleminfo =
                new XmsCliTelemInfoParser().ParseXMsTelemHeader(responseHeaders["x-ms-clitelem"], _requestContext);

            // Assert
            Assert.AreEqual(xmsCliTeleminfo.Version, "1");
            Assert.AreEqual(xmsCliTeleminfo.ServerErrorCode, "0");
            Assert.AreEqual(xmsCliTeleminfo.ServerSubErrorCode, "0");
            Assert.AreEqual(xmsCliTeleminfo.TokenAge, "");
            Assert.AreEqual(xmsCliTeleminfo.SpeInfo, "");
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void XmsClientTelemInfoParseTest_IncorrectFormat()
        {
            //Act - Parse malformed header - 6 values
            var responseHeaders = new Dictionary<string, string>
            {
                {"x-ms-clitelem", "1,2,3,4,5,6"}
            };

            var xmsCliTeleminfo =
                new XmsCliTelemInfoParser().ParseXMsTelemHeader(responseHeaders["x-ms-clitelem"], _requestContext);

            // Assert
            _coreLogger.Received().Warning(
                Arg.Is(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        TelemetryError.XmsCliTelemMalformed,
                        responseHeaders["x-ms-clitelem"])));
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void XmsClientTelemInfoParseTest_IncorrectHeaderVersion()
        {
            //Act - Parse wrong version of header - should be "1"
            var responseHeaders = new Dictionary<string, string>
            {
                {"x-ms-clitelem", "3,0,0,,"}
            };

            var xmsCliTeleminfo =
                new XmsCliTelemInfoParser().ParseXMsTelemHeader(responseHeaders["x-ms-clitelem"], _requestContext);

            // Assert
            _coreLogger.Received().Warning(
                Arg.Is(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        TelemetryError.XmsUnrecognizedHeaderVersion,
                        "3")));
        }
    }
}