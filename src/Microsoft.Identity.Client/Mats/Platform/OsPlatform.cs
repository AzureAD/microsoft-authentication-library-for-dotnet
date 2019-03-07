// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Platform
{
    internal enum OsPlatform
    {
        Win32,
        Android,
        Ios,
        Mac,
        Winrt
    }

    internal static class OsPlatformUtils
    {
        public static string AsString(OsPlatform osPlatform)
        {
            switch (osPlatform)
            {
            case OsPlatform.Win32:
                return "win32";

            case OsPlatform.Android:
                return "android";

            case OsPlatform.Ios:
                return "ios";

            case OsPlatform.Mac:
                return "mac";

            case OsPlatform.Winrt:
                return "winrt";

            default:
                return "unknown";
            }
        }

        public static int AsInt(OsPlatform osPlatform)
        {
            return Convert.ToInt32(osPlatform, CultureInfo.InvariantCulture);
        }
    }
}
