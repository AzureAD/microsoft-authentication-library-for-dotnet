// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class PropertyBagContents
    {
        public PropertyBagContents(EventType eventType)
        {
            EventType = eventType;
        }

        public EventType EventType {get;}

        public readonly Dictionary<string, string> StringProperties = new Dictionary<string, string>();
        public readonly Dictionary<string, int> IntProperties = new Dictionary<string, int>();
        public readonly Dictionary<string, long> Int64Properties = new Dictionary<string, long>();
        public readonly Dictionary<string, bool> BoolProperties = new Dictionary<string, bool>();
    }
}
