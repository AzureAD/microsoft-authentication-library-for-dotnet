// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Identity.Client.TelemetryCore.Http;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class HttpHeaderSantizerTests
    {
        [TestMethod]
        // For https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1881
        public void SantizerAllowsStrangeErrorCodesToBeSentAsHeaders()
        {
            // this is a real B2C error code :(
            string strangeErrorCode = "An error occured, error: access_denied, error_description: AADB2C90118: The user has forgotten their password. "
                + Environment.NewLine +
                "Correlation Id: " + Guid.NewGuid()
                + Environment.NewLine +
                "Timestamp " + DateTime.UtcNow
                + Environment.NewLine +
                "An error occured, error: access_denied, error_description: AADB2C90118: The user has forgotten their password." +
                "Correlation Id: " + Guid.NewGuid()
                + Environment.NewLine +
                "Timestamp " + DateTime.UtcNow;
            HttpRequestMessage httpRequest = new HttpRequestMessage();

            // newline chars must be followed by spaces
            Assert.ThrowsException<FormatException>(() => httpRequest.Headers.Add("x-client-last-telemetry", strangeErrorCode));
            
            string santized = HttpHeaderSantizer.SantizeHeader(strangeErrorCode);
            httpRequest.Headers.Add("x-client-last-telemetry", santized);
        }
    }
}
