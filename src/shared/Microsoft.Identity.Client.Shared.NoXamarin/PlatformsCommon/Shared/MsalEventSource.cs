// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.Tracing;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    [EventSource(Name = "Microsoft.Identity.Client")]
    internal class MsalEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Verbose)]
        internal void Verbose(string message)
        {
            WriteEvent(1, message);
        }

        [Event(2, Level = EventLevel.Informational)]
        internal void Information(string message)
        {
            WriteEvent(2, message);
        }

        [Event(3, Level = EventLevel.Warning)]
        internal void Warning(string message)
        {
            WriteEvent(3, message);
        }

        [Event(4, Level = EventLevel.Error)]
        internal void Error(string message)
        {
            WriteEvent(4, message);
        }
    }
}
