// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class MatsTelemetryBatch : IMatsTelemetryBatch
    {
        private readonly Dictionary<string, bool> _boolData = new Dictionary<string, bool>();
        private readonly Dictionary<string, long> _int64Data = new Dictionary<string, long>();
        private readonly Dictionary<string, int> _intData = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _stringData = new Dictionary<string, string>();

        public static IMatsTelemetryBatch Create(string name, PropertyBagContents contents)
        {
            var batch = new MatsTelemetryBatch(name);
            batch.SetStringData(contents.StringProperties);
            batch.SetIntData(contents.IntProperties);
            batch.SetInt64Data(contents.Int64Properties);
            batch.SetBoolData(contents.BoolProperties);

            return batch;
        }

        private void SetBoolData(ConcurrentDictionary<string, bool> data)
        {
            _boolData.Clear();
            foreach (var kvp in data)
            {
                _boolData[kvp.Key] = kvp.Value;
            }
        }

        private void SetInt64Data(ConcurrentDictionary<string, long> data)
        {
            _int64Data.Clear();
            foreach (var kvp in data)
            {
                _int64Data[kvp.Key] = kvp.Value;
            }
        }

        private void SetIntData(ConcurrentDictionary<string, int> data)
        {
            _intData.Clear();
            foreach (var kvp in data)
            {
                _intData[kvp.Key] = kvp.Value;
            }
        }

        private void SetStringData(ConcurrentDictionary<string, string> data)
        {
            _stringData.Clear();
            foreach (var kvp in data)
            {
                _stringData[kvp.Key] = kvp.Value;
            }
        }

        private MatsTelemetryBatch(string name)
        {
            Name = name;
        }

        public IReadOnlyDictionary<string, bool> BoolValues => _boolData;
        public IReadOnlyDictionary<string, long> Int64Values => _int64Data;
        public IReadOnlyDictionary<string, int> IntValues => _intData;
        public IReadOnlyDictionary<string, string> StringValues => _stringData;

        public string Name { get; }
    }
}
