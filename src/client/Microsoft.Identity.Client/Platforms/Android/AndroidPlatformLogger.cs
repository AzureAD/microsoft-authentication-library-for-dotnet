// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Android.Util;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal class AndroidPlatformLogger : IPlatformLogger
    {
        public void Error(string errorMessage)
        {
            Log.Error(null, errorMessage);
        }

        public void Warning(string message)
        {
            Log.Warn(null, message);
        }

        public void Verbose(string message)
        {
            Log.Verbose(null, message);
        }

        public void Information(string message)
        {
            Log.Info(null, message);
        }
    }
}
