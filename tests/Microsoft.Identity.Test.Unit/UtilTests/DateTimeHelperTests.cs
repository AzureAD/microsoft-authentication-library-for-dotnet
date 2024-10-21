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
        public void TestGetDurationFromNowInSecondsForUnixTimestampOnly()
        {
            // Arrange
            var currentTime = DateTimeOffset.UtcNow;

            // Example 1: Valid Unix timestamp (seconds since epoch)
            long currentUnixTimestamp = DateTimeHelpers.CurrDateTimeInUnixTimestamp(); // e.g., 1697490590
            string unixTimestampString = currentUnixTimestamp.ToString(CultureInfo.InvariantCulture);
            long result = DateTimeHelpers.GetDurationFromNowInSeconds(unixTimestampString);
            Assert.IsTrue(result >= 0, "Valid Unix timestamp (seconds) failed");

            // Example 2: Unix timestamp in the future
            string futureUnixTimestamp = (currentUnixTimestamp + 3600).ToString(); // 1 hour from now
            result = DateTimeHelpers.GetDurationFromNowInSeconds(futureUnixTimestamp);
            Assert.IsTrue(result > 0, "Future Unix timestamp failed");

            // Example 3: Unix timestamp in the past
            string pastUnixTimestamp = (currentUnixTimestamp - 3600).ToString(); // 1 hour ago
            result = DateTimeHelpers.GetDurationFromNowInSeconds(pastUnixTimestamp);
            Assert.IsTrue(result < 0, "Past Unix timestamp failed");

            // Example 4: Empty string (should return 0)
            string emptyString = string.Empty;
            result = DateTimeHelpers.GetDurationFromNowInSeconds(emptyString);
            Assert.AreEqual(0, result, "Empty string did not return 0 as expected.");
        }

        [TestMethod]
        public void TestGetDurationFromNowInSecondsFromManagedIdentity()
        {
            // Arrange
            var currentTime = DateTimeOffset.UtcNow;

            // Example 1: Unix timestamp (seconds since epoch)
            string unixTimestampInSeconds = DateTimeHelpers.DateTimeToUnixTimestamp(currentTime); // e.g., 1697490590
            long result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(unixTimestampInSeconds);
            Assert.IsTrue(result >= 0, "Unix timestamp (seconds) failed");

            // Example 2: ISO 8601 format
            string iso8601 = currentTime.ToString("o", CultureInfo.InvariantCulture); // e.g., 2024-10-18T19:51:37.0000000+00:00
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(iso8601);
            Assert.IsTrue(result >= 0, "ISO 8601 failed");

            // Example 3: Common format (MM/dd/yyyy HH:mm:ss)
            string commonFormat1 = currentTime.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture); // e.g., 10/18/2024 19:51:37
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(commonFormat1);
            Assert.IsTrue(result >= 0, "Common Format 1 failed");

            // Example 4: Common format (yyyy-MM-dd HH:mm:ss)
            string commonFormat2 = currentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); // e.g., 2024-10-18 19:51:37
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(commonFormat2);
            Assert.IsTrue(result >= 0, "Common Format 2 failed");

            // Example 5: Invalid format (should throw an MsalClientException)
            string invalidFormat = "invalid-date-format";
            Assert.ThrowsException<MsalClientException>(() =>
            {
                DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(invalidFormat);
            }, "Invalid format did not throw an exception as expected.");
        }
    }
}
