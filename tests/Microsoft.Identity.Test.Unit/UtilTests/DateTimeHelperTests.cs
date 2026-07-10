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
            Assert.IsGreaterThanOrEqualTo(-1, result, "Valid Unix timestamp (seconds) failed");

            // Example 2: Unix timestamp in the future
            string futureUnixTimestamp = (currentUnixTimestamp + 3600).ToString(); // 1 hour from now
            result = DateTimeHelpers.GetDurationFromNowInSeconds(futureUnixTimestamp);
            Assert.IsGreaterThan(0, result, "Future Unix timestamp failed");

            // Example 3: Unix timestamp in the past
            string pastUnixTimestamp = (currentUnixTimestamp - 3600).ToString(); // 1 hour ago
            result = DateTimeHelpers.GetDurationFromNowInSeconds(pastUnixTimestamp);
            Assert.IsLessThan(0, result, "Past Unix timestamp failed");

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
            Assert.IsGreaterThanOrEqualTo(-1, result, "Unix timestamp (seconds) failed");

            // Example 2: ISO 8601 format
            string iso8601 = currentTime.ToString("o", CultureInfo.InvariantCulture); // e.g., 2024-10-18T19:51:37.0000000+00:00
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(iso8601);
            Assert.IsGreaterThanOrEqualTo(-1, result, "ISO 8601 failed");

            // Example 3: Common format (MM/dd/yyyy HH:mm:ss)
            string commonFormat1 = currentTime.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture); // e.g., 10/18/2024 19:51:37
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(commonFormat1);
            Assert.IsGreaterThanOrEqualTo(-1, result, "Common Format 1 failed");

            // Example 4: Common format (yyyy-MM-dd HH:mm:ss) — no timezone indicator, parsed as local time.
            // Use a timestamp well in the future so local-time offset doesn't cause a near-zero result
            // to flip negative on machines where local time is ahead of UTC.
            string commonFormat2 = (currentTime + TimeSpan.FromHours(2)).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(commonFormat2);
            Assert.IsGreaterThan(0, result, $"Common Format 2 with a future timestamp should return positive duration, got {result}.");

            // Example 5: Invalid format (should throw an MsalClientException)
            string invalidFormat = "invalid-date-format";
            Assert.Throws<MsalClientException>(() =>
            {
                DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(invalidFormat);
            }, "Invalid format did not throw an exception as expected.");
        }

        [TestMethod]
        public void TestGetDurationFromManagedIdentityTimestamp_PastTimestamp_ReturnsZero()
        {
            // A past absolute Unix timestamp must return 0 so the caller treats the
            // response as expired and re-fetches, rather than caching the token with
            // the raw epoch value as a lifetime (which would be decades).

            long currentUnix = DateTimeHelpers.CurrDateTimeInUnixTimestamp();

            // 1 hour in the past
            string pastByOneHour = (currentUnix - 3600).ToString(CultureInfo.InvariantCulture);
            long result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(pastByOneHour);
            Assert.AreEqual(0, result, "A timestamp 1 hour in the past should return 0.");

            // 24 hours in the past (typical IMDS token lifetime already elapsed)
            string pastByOneDay = (currentUnix - 86400).ToString(CultureInfo.InvariantCulture);
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(pastByOneDay);
            Assert.AreEqual(0, result, "A timestamp 24 hours in the past should return 0.");

            // Well in the past (2024-era absolute timestamp)
            string ancientTimestamp = "1697490590"; // 2023-10-17 — always in the past
            result = DateTimeHelpers.GetDurationFromManagedIdentityTimestamp(ancientTimestamp);
            Assert.AreEqual(0, result, "A well-past absolute timestamp should return 0, not the raw epoch value.");
        }
    }
}
