// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class MatsTelemetryData : IMatsTelemetryData
    {
        private readonly PropertyBagContents _contents;
        public MatsTelemetryData(string name, PropertyBagContents contents)
        {
            Name = name;
            _contents = contents;
        }

        public string Name {get;}

        public Dictionary<string, bool> GetBoolMap() => _contents.BoolProperties;
        public Dictionary<string, long> GetInt64Map() => _contents.Int64Properties;
        public Dictionary<string, int> GetIntMap() => _contents.IntProperties;
        public Dictionary<string, string> GetStringMap() => _contents.StringProperties;
    }
}
