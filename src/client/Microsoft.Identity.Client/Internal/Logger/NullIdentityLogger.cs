// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Client.Internal.Logger
{
    internal class NullIdentityLogger : IIdentityLogger
    {
        public bool IsEnabled(EventLogLevel eventLogLevel)
        {
            return false;
        }

        public void Log(LogEntry entry)
        {
            //No Op
        }
    }
}
