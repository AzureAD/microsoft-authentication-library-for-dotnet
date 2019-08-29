// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal interface IPropertyBag
    {
        PropertyBagContents GetContents();
        void Add(string key, string val);
        void Add(string key, int val);
        void Add(string key, long val);
        void Update(string key, int value);
    }
}
