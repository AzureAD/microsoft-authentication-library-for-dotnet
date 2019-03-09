// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal static class UploadEventUtils
    {
        public static string GetUploadEventName(IPlatformProxy platformProxy, EventType eventType, string appName)
        {
            string eventName = MatsConverter.AsString(eventType);
            string osPlatform = platformProxy.GetMatsOsPlatform();

            return GetUploadEventNameGeneric(
                eventName.ToUpperInvariant(), 
                appName.ToUpperInvariant(), 
                osPlatform.ToUpperInvariant());
        }

        private static string GetUploadEventNameGeneric(
            string eventName, 
            string appName, 
            string osPlatform)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", eventName, appName, osPlatform);
        }
    }
}
