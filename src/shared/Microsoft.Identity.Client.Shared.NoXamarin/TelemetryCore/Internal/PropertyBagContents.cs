// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class PropertyBagContents
    {
        public PropertyBagContents(EventType eventType)
        {
            EventType = eventType;
        }

        public EventType EventType {get;}

        public readonly ConcurrentDictionary<string, string> StringProperties = new ConcurrentDictionary<string, string>();
        public readonly ConcurrentDictionary<string, int> IntProperties = new ConcurrentDictionary<string, int>();
        public readonly ConcurrentDictionary<string, long> Int64Properties = new ConcurrentDictionary<string, long>();
        public readonly ConcurrentDictionary<string, bool> BoolProperties = new ConcurrentDictionary<string, bool>();
    }
}
