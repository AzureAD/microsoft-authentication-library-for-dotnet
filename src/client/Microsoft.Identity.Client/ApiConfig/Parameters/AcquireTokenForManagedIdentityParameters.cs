﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenForManagedIdentityParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }

        public string Resource { get; set; }

        public void LogParameters(ILoggerAdapter logger)
        {
            if (logger.IsLoggingEnabled(LogLevel.Info))
            {
                logger.Info(
                    $"""
                     === AcquireTokenForManagedIdentityParameters ===
                     ForceRefresh: {ForceRefresh}
                     Resource: {Resource}
                     """);
            }
        }
    }
}
