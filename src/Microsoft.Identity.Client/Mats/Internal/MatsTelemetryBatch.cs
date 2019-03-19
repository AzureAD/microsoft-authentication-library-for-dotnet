// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class MatsTelemetryBatch : IMatsTelemetryBatch
    {
        private readonly Dictionary<string, bool> _boolData = new Dictionary<string, bool>();
        private readonly Dictionary<string, long> _int64Data = new Dictionary<string, long>();
        private readonly Dictionary<string, int> _intData = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _stringData = new Dictionary<string, string>();


        public static IMatsTelemetryBatch Create(IMatsTelemetryData data)
        {
            var batch = new MatsTelemetryBatch(data.Name);
            batch.SetStringData(data.GetStringMap());
            batch.SetIntData(data.GetIntMap());
            batch.SetInt64Data(data.GetInt64Map());
            batch.SetBoolData(data.GetBoolMap());

            return batch;
        }

        private void SetBoolData(Dictionary<string, bool> data)
        {
            _boolData.Clear();
            foreach (var kvp in data)
            {
                _boolData[kvp.Key] = kvp.Value;
            }
        }

        private void SetInt64Data(Dictionary<string, long> data)
        {
            _int64Data.Clear();
            foreach (var kvp in data)
            {
                _int64Data[kvp.Key] = kvp.Value;
            }
        }

        private void SetIntData(Dictionary<string, int> data)
        {
            _intData.Clear();
            foreach (var kvp in data)
            {
                _intData[kvp.Key] = kvp.Value;
            }
        }

        private void SetStringData(Dictionary<string, string> data)
        {
            _stringData.Clear();
            foreach (var kvp in data)
            {
                _stringData[kvp.Key] = kvp.Value;
            }
        }

        private readonly string _name;

        private MatsTelemetryBatch(string name)
        {
            _name = name;
        }

        public IReadOnlyDictionary<string, bool> BoolValues => _boolData;
        public IReadOnlyDictionary<string, long> Int64Values => _int64Data;
        public IReadOnlyDictionary<string, int> IntValues => _intData;
        public IReadOnlyDictionary<string, string> StringValues => _stringData;

        public string GetName() => _name;
    }
}
