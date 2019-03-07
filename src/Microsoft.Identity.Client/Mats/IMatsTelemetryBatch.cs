// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats
{
    internal interface IMatsTelemetryBatch
    {
        string GetName();

        int GetStringRowCount();
        string GetStringKey(int index);
        string GetStringValue(int index);

        int GetIntRowCount();
        string GetIntKey(int index);
        int GetIntValue(int index);

        int GetInt64RowCount();
        string GetInt64Key(int index);
        long GetInt64Value(int index);

        int GetBoolRowCount();
        string GetBoolKey(int index);
        bool GetBoolValue(int index);
    }
}
