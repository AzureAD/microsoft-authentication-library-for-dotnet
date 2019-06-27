// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Instance;
using System.Globalization;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;

namespace Microsoft.Identity.Client.TelemetryCore.Internal.Events
{
    internal abstract class EventBase : Dictionary<string, string>
    {
        protected const string EventNamePrefix = "msal.";
        public const string EventNameKey = EventNamePrefix + "event_name";
        public const string StartTimeKey = EventNamePrefix + "start_time";
        public const string ElapsedTimeKey = EventNamePrefix + "elapsed_time";
        private readonly long _startTimestamp;
        public const string TenantPlaceHolder = "<tenant>"; // It is used to replace the real tenant in telemetry info

        public EventBase(string eventName, string correlationId)
        {
            this[EventNameKey] = eventName;
            _startTimestamp = CurrentUnixTimeMilliseconds();
            this[StartTimeKey] = _startTimestamp.ToString(CultureInfo.InvariantCulture);
            this[ElapsedTimeKey] = "-1";
            CorrelationId = correlationId;
            EventId = Guid.NewGuid().AsMatsCorrelationId();  // used to uniquely identify this particular event index for start/stop matching.
        }

        public string EventId { get; }

        protected static long CurrentUnixTimeMilliseconds()
        {
            return CoreHelpers.DateTimeToUnixTimestampMilliseconds(DateTimeOffset.Now);
        }

        public string CorrelationId
        {
            get => this[MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey];
            set => this[MsalTelemetryBlobEventNames.MsalCorrelationIdConstStrKey] = value;
        }

        public void Stop()
        {
            this[ElapsedTimeKey] = (CurrentUnixTimeMilliseconds() - _startTimestamp).ToString(CultureInfo.InvariantCulture);  // It is a duration
        }

        public static string ScrubTenant(Uri uri) // Note: There is also a Unit Test case for this helper
        {
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException("Requires an absolute uri");
            }
            if (!AadAuthority.IsInTrustedHostList(uri.Host)) // only collect telemetry for well-known hosts
            {
                return null;
            }

            var pieces = uri.AbsolutePath.Split('/'); // It looks like {"", "common", "oauth2", "v2.0", "token"}
            if (pieces.Length >= 2)
            {
                int tenantPosition = pieces[1] == B2CAuthority.Prefix ? 2 : 1;
                if (tenantPosition < pieces.Length)
                {
                    // Replace it rather than remove it. Otherwise the end result would misleadingly look like a complete URL while it is actually not.
                    pieces[tenantPosition] = TenantPlaceHolder;
                }
            }
            string scrubbedPath = string.Join("/", pieces);
            return uri.Scheme + "://" + uri.Authority + scrubbedPath;
        }

        public string HashPersonalIdentifier(ICryptographyManager cryptographyManager, string valueToHash)
        {
            return cryptographyManager.CreateBase64UrlEncodedSha256Hash(valueToHash);
        }
    }
}
