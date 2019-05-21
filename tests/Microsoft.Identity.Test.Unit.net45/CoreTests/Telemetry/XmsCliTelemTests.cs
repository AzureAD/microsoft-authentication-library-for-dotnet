// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.CoreTests.Telemetry
{
    [TestClass]
    public class XmsCliTelemTests
    {
        private ICoreLogger _coreLogger;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();

            // Methods in XmsCliTelemTests log errors when parsing response headers;
            _coreLogger = Substitute.For<ICoreLogger>();
        }

        [TestMethod]
        [TestCategory("TelemetryInternalAPI")]
        public void XmsClientTelemInfoParseTest_XmsCliTelemInfoCorrectFormat()
        {
            // Act - Parse correctly formatted header
            var responseHeaders = new Dictionary<string, string>
            {
                {"x-ms-clitelem", "1,0,0,,"}
            };

            var xmsCliTeleminfo =
                new XmsCliTelemInfoParser().ParseXMsTelemHeader(responseHeaders["x-ms-clitelem"], _coreLogger);

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
            // Act - Parse malformed header - 6 values
            var responseHeaders = new Dictionary<string, string>
            {
                {"x-ms-clitelem", "1,2,3,4,5,6"}
            };

            var xmsCliTeleminfo =
                new XmsCliTelemInfoParser().ParseXMsTelemHeader(responseHeaders["x-ms-clitelem"], _coreLogger);

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
            // Act - Parse wrong version of header - should be "1"
            var responseHeaders = new Dictionary<string, string>
            {
                {"x-ms-clitelem", "3,0,0,,"}
            };

            var xmsCliTeleminfo =
                new XmsCliTelemInfoParser().ParseXMsTelemHeader(responseHeaders["x-ms-clitelem"], _coreLogger);

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
