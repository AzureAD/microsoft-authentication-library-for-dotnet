// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal interface IMatsTelemetryData
    {
        // Get the name of the event to upload
        string Name {get;}

        // Returns the string properties of the event.
        Dictionary<string, string> GetStringMap();

        // Returns the int32 contents of the event.
        Dictionary<string, int> GetIntMap();

        // Returns the int64 contents of the event.
        Dictionary<string, long> GetInt64Map();

        // Returns the bool contents of the event.
        Dictionary<string, bool> GetBoolMap();
    }
}
