// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenWithDeviceCodeParameters : IAcquireTokenParameters
    {
        public Func<DeviceCodeResult, Task> DeviceCodeResultCallback { get; set; }

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
        {
        }
    }
}
