// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal class EventSourcePlatformLogger : IPlatformLogger
    {
        static EventSourcePlatformLogger()
        {
            MsalEventSource = new MsalEventSource();
        }

        internal static MsalEventSource MsalEventSource { get; }

        public void Error(string message)
        {
            MsalEventSource.Error(message);
        }

        public void Warning(string message)
        {
            MsalEventSource.Error(message);
        }

        public void Verbose(string message)
        {
            MsalEventSource.Error(message);
        }

        public void Information(string message)
        {
            MsalEventSource.Error(message);
        }
    }
}
