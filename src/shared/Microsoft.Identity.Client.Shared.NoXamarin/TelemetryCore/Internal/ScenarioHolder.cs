// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal class ScenarioHolder
    {
        public ScenarioHolder(IPropertyBag propertyBag)
        {
            PropertyBag = propertyBag;
            StartTime = DateTime.UtcNow;
            ShouldUpload = false;
        }

        public IPropertyBag PropertyBag {get;}
        public bool ShouldUpload {get; set;}
        public DateTime StartTime {get;}
    }
}
