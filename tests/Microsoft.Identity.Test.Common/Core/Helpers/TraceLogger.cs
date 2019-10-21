// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal class TraceLogger : ICoreLogger
    {
        private readonly string _prefix;

        public TraceLogger(string prefix)
        {
            _prefix = prefix;
        }

        public Guid CorrelationId => Guid.Empty;

        public string ClientName => "MSAL.Test";

        public string ClientVersion => "0";

        public bool PiiLoggingEnabled => true;

        public void Error(string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[Error] {messageScrubbed}");
        }

        public void ErrorPii(string messageWithPii, string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[ErrorPii] {messageWithPii}");
        }

        public void ErrorPii(Exception exWithPii)
        {
            Trace.WriteLine($"{_prefix}[Exception] Exception {exWithPii.Message} - {exWithPii}");
        }

        public void ErrorPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Trace.WriteLine($"{_prefix}[Exception] {prefix} Exception {exWithPii.Message} - {exWithPii}");
        }

        public void Info(string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[Info] {messageScrubbed}");
        }

        public void InfoPii(string messageWithPii, string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[InfoPii] {messageWithPii}");
        }

        public void InfoPii(Exception exWithPii)
        {
            Trace.WriteLine($"{_prefix}[Exception] Exception {exWithPii.Message} - {exWithPii}");
        }

        public void InfoPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Trace.WriteLine($"{_prefix}[Exception] Exception {exWithPii.Message} - {exWithPii}");
        }

        public void Verbose(string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[Verbose] {messageScrubbed}");

        }

        public void VerbosePii(string messageWithPii, string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[VerbosePii] {messageScrubbed}");
        }

        public void Warning(string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[Warning] {messageScrubbed}");
        }

        public void WarningPii(string messageWithPii, string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[WarningPii] {messageWithPii}");
        }

        public void WarningPii(Exception exWithPii)
        {
            Trace.WriteLine($"{_prefix}[Exception] Exception {exWithPii.Message} - {exWithPii}");
        }

        public void WarningPiiWithPrefix(Exception exWithPii, string prefix)
        {
            Trace.WriteLine($"{_prefix}[Exception] Exception {exWithPii.Message} - {exWithPii}");
        }
    }
}
