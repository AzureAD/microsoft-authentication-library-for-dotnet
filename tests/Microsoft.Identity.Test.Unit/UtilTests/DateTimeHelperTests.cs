// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class DateTimeHelperTests
    {
        [TestMethod]
        public void TestGetDurationFromNowInSeconds()
        {
            // Arrange
            var currentTime = DateTimeOffset.UtcNow;

            // Unix timestamp (seconds since epoch)
            string unixTimestamp = DateTimeHelpers.DateTimeToUnixTimestamp(currentTime);
            long result = DateTimeHelpers.GetDurationFromNowInSeconds(unixTimestamp);
            Assert.IsTrue(result >= 0, "Unix timestamp failed");

            // ISO 8601 format
            string iso8601 = currentTime.ToString("o", CultureInfo.InvariantCulture); // ISO 8601 with "o" format
            result = DateTimeHelpers.GetDurationFromNowInSeconds(iso8601);
            Assert.IsTrue(result >= 0, "ISO 8601 failed");

            // RFC 1123 format
            string rfc1123 = currentTime.ToString("R", CultureInfo.InvariantCulture); // RFC 1123 with "R" format
            result = DateTimeHelpers.GetDurationFromNowInSeconds(rfc1123);
            Assert.IsTrue(result >= 0, "RFC 1123 failed");

            // Common format: MM/dd/yyyy HH:mm:ss
            string commonFormat1 = currentTime.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            result = DateTimeHelpers.GetDurationFromNowInSeconds(commonFormat1);
            Assert.IsTrue(result >= 0, "Common Format 1 failed");

            // Common format: yyyy-MM-dd HH:mm:ss
            string commonFormat2 = currentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            result = DateTimeHelpers.GetDurationFromNowInSeconds(commonFormat2);
            Assert.IsTrue(result >= 0, "Common Format 2 failed");

            // Invalid format: This should throw an MsalClientException
            string invalidFormat = "invalid-date-format";
            Assert.ThrowsException<MsalClientException>(() =>
            {
                DateTimeHelpers.GetDurationFromNowInSeconds(invalidFormat);
            });
        }
    }
}
