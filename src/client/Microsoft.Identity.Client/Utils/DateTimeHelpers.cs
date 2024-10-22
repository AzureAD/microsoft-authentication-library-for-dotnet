// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Utils
{
    internal static class DateTimeHelpers
    {
        public static DateTimeOffset UnixTimestampToDateTime(double unixTimestamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimestamp).ToUniversalTime();
            return dateTime;
        }

        public static DateTimeOffset? UnixTimestampToDateTimeOrNull(double unixTimestamp)
        {
            if (unixTimestamp == 0)
                return null;

            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimestamp).ToUniversalTime();
            return dateTime;
        }

        public static string DateTimeToUnixTimestamp(DateTimeOffset dateTimeOffset)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimestamp = (long)dateTimeOffset.Subtract(dateTime).TotalSeconds;
            return unixTimestamp.ToString(CultureInfo.InvariantCulture);
        }

        public static long CurrDateTimeInUnixTimestamp()
        {
            var unixEpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimestamp = (long)DateTime.UtcNow.Subtract(unixEpochDateTime).TotalSeconds;
            return unixTimestamp;
        }

        public static long GetDurationFromWindowsTimestamp(string windowsTimestampInFuture, ILoggerAdapter logger)
        {
            if (string.IsNullOrEmpty(windowsTimestampInFuture))
            {
                return 0;
            }

            // Windows uses in most functions the FILETIME structure, which represents the actual time as the number of 100-nanosecond intervals since January 1, 1601 (UTC).
            // To convert to unix timestamp (Jan 1, 1970), you have to subtract 11644473600 seconds.

            if (!ulong.TryParse(windowsTimestampInFuture, out ulong winTimestamp) ||
                winTimestamp <= 11644473600 ||
                winTimestamp == ulong.MaxValue)
            {
                logger.Warning("Invalid Universal time " + windowsTimestampInFuture);
                return 0;
            }

            ulong unixTimestamp = winTimestamp - 11644473600;

            return (long)unixTimestamp - CurrDateTimeInUnixTimestamp();
        }

        public static long GetDurationFromNowInSeconds(string unixTimestampInFuture)
        {
            if (string.IsNullOrEmpty(unixTimestampInFuture))
            {
                return 0;
            }

            long expiresOnUnixTimestamp = long.Parse(unixTimestampInFuture, CultureInfo.InvariantCulture);
            return expiresOnUnixTimestamp - CurrDateTimeInUnixTimestamp();
        }

        public static long GetDurationFromManagedIdentityTimestamp(string dateTimeStamp)
        {
            if (string.IsNullOrEmpty(dateTimeStamp))
            {
                return 0;
            }

            // First, try to parse as Unix timestamp (number of seconds since epoch)
            // Example: "1697490590" (Unix timestamp representing seconds since 1970-01-01)
            if (long.TryParse(dateTimeStamp, out long expiresOnUnixTimestamp))
            {
                return expiresOnUnixTimestamp - DateTimeHelpers.CurrDateTimeInUnixTimestamp();
            }

            // Try parsing as ISO 8601 
            // Example: "2024-10-18T19:51:37.0000000+00:00" (ISO 8601 format)
            if (DateTimeOffset.TryParse(dateTimeStamp, null, DateTimeStyles.RoundtripKind, out DateTimeOffset expiresOnDateTime))
            {
                return (long)(expiresOnDateTime - DateTimeOffset.UtcNow).TotalSeconds;
            }

            // If no format works, throw an MSAL client exception
            throw new MsalClientException("invalid_timestamp_format", $"Failed to parse date-time stamp from identity provider. Invalid format: '{dateTimeStamp}'.");
        }

        public static DateTimeOffset? DateTimeOffsetFromDuration(long? duration)
        {
            if (duration.HasValue)
                return DateTimeOffsetFromDuration(duration.Value);

            return null;
        }

        public static DateTimeOffset DateTimeOffsetFromDuration(long duration)
        {
            return DateTime.UtcNow + TimeSpan.FromSeconds(duration);
        }
    }
}
