// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Core
{
    internal interface ICoreLogger
    {
        Guid CorrelationId { get; }
        string ClientName { get; }
        string ClientVersion { get; }
        bool PiiLoggingEnabled { get; }
        void Error(string messageScrubbed);
        void ErrorPii(string messageWithPii, string messageScrubbed);
        void ErrorPii(Exception exWithPii);
        void ErrorPiiWithPrefix(Exception exWithPii, string prefix);
        void Warning(string messageScrubbed);
        void WarningPii(string messageWithPii, string messageScrubbed);
        void WarningPii(Exception exWithPii);
        void WarningPiiWithPrefix(Exception exWithPii, string prefix);
        void Info(string messageScrubbed);
        void InfoPii(string messageWithPii, string messageScrubbed);
        void InfoPii(Exception exWithPii);
        void InfoPiiWithPrefix(Exception exWithPii, string prefix);
        void Verbose(string messageScrubbed);
        void VerbosePii(string messageWithPii, string messageScrubbed);
    }
}
