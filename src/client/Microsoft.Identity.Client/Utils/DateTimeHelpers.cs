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

            // First, try to parse as a numeric value. This covers two shapes:
            //
            // 1. Absolute Unix timestamp ("expires_on"): a large epoch value such as
            //    "1697490590" (2023-10-17). Values at or above the year-2001 epoch
            //    (978307200) are treated as absolute timestamps; the remaining time is
            //    computed and clamped to 0 for past values so stale tokens are re-fetched.
            //
            // 2. Relative lifetime ("expires_in"): a small number of seconds such as
            //    "3600". Values below the year-2001 threshold are returned as-is because
            //    they represent seconds-from-now, not an epoch point.
            //
            // The threshold 978307200 = 2001-01-01T00:00:00Z, well below any real
            // future `expires_on` and well above any reasonable `expires_in` value.
            const long AbsoluteTimestampThreshold = 978307200L;

            if (long.TryParse(dateTimeStamp, out long expiresOnUnixTimestamp))
            {
                if (expiresOnUnixTimestamp < AbsoluteTimestampThreshold)
                {
                    // Relative lifetime (expires_in): return directly.
                    return expiresOnUnixTimestamp;
                }

                // Absolute timestamp (expires_on): compute remaining seconds.
                // Return 0 for past values so the caller treats this as expired.
                long remaining = expiresOnUnixTimestamp - DateTimeHelpers.CurrDateTimeInUnixTimestamp();
                return remaining < 0 ? 0 : remaining;
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
