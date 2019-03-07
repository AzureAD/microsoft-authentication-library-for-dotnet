// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal class MatsTelemetryBatch : IMatsTelemetryBatch
    {
        public static IMatsTelemetryBatch Create(IMatsTelemetryData data)
        {
            var batch = new MatsTelemetryBatch(data.Name);
            batch.SetStringData(data.GetStringMap());
            batch.SetIntData(data.GetIntMap());
            batch.SetInt64Data(data.GetInt64Map());
            batch.SetBoolData(data.GetBoolMap());

            return batch;
        }

        private void SetBoolData(Dictionary<string, bool> data) => throw new NotImplementedException();
        private void SetInt64Data(Dictionary<string, long> data) => throw new NotImplementedException();
        private void SetIntData(Dictionary<string, int> data) => throw new NotImplementedException();
        private void SetStringData(Dictionary<string, string> data) => throw new NotImplementedException();

        private readonly string _name;

        private MatsTelemetryBatch(string name)
        {
            _name = name;
        }

        public string GetBoolKey(int index) => throw new NotImplementedException();
        public int GetBoolRowCount() => throw new NotImplementedException();
        public bool GetBoolValue(int index) => throw new NotImplementedException();
        public string GetInt64Key(int index) => throw new NotImplementedException();
        public int GetInt64RowCount() => throw new NotImplementedException();
        public long GetInt64Value(int index) => throw new NotImplementedException();
        public string GetIntKey(int index) => throw new NotImplementedException();
        public int GetIntRowCount() => throw new NotImplementedException();
        public int GetIntValue(int index) => throw new NotImplementedException();
        public string GetName() => _name;
        public string GetStringKey(int index) => throw new NotImplementedException();
        public int GetStringRowCount() => throw new NotImplementedException();
        public string GetStringValue(int index) => throw new NotImplementedException();
    }
}
