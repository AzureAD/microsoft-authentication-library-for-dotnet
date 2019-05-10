// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Internal
{
    internal class NullLogger : ICoreLogger
    {
        public string ClientName { get; } = string.Empty;
        public string ClientVersion { get; } = string.Empty;

        public Guid CorrelationId { get; } = Guid.Empty;
        public bool PiiLoggingEnabled { get; } = false;

        public void Error(string messageScrubbed)
        {
        }

        public void ErrorPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void ErrorPii(Exception exWithPii)
        {
        }

        public void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Warning(string messageScrubbed)
        {
        }

        public void WarningPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void WarningPii(Exception exWithPii)
        {
        }

        public void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Info(string messageScrubbed)
        {
        }

        public void InfoPii(string messageWithPii, string messageScrubbed)
        {
        }

        public void InfoPii(Exception exWithPii)
        {
        }

        public void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
        }

        public void Verbose(string messageScrubbed)
        {
        }

        public void VerbosePii(string messageWithPii, string messageScrubbed)
        {
        }
    }
}
