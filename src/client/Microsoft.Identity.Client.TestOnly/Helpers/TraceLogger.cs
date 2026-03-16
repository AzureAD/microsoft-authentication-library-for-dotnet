// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.IdentityModel.Abstractions;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal class TraceLogger : ILoggerAdapter
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

        public bool IsDefaultPlatformLoggingEnabled => false;

        public IIdentityLogger CacheLogger => null;

        public IIdentityLogger IdentityLogger => throw new NotImplementedException();

        public void Always(string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[Always] {messageScrubbed}");
        }

        public void AlwaysPii(string messageWithPii, string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[AlwaysPii] {messageWithPii}");
        }

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

        public bool IsLoggingEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log(LogLevel msalLogLevel, string messageWithPii, string messageScrubbed)
        {
            Trace.WriteLine($"{_prefix}[{msalLogLevel}] {messageWithPii ?? messageScrubbed}");
        }

        public DurationLogHelper LogBlockDuration(string measuredBlockName, LogLevel logLevel = LogLevel.Verbose)
        {
            return new DurationLogHelper(this, measuredBlockName, logLevel);
        }

        public DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null)
        {
            return LogBlockDuration(methodName, logLevel);
        }

        public DurationLogHelper LogMethodDuration(LogLevel logLevel = LogLevel.Verbose, [CallerMemberName] string methodName = null, [CallerFilePath] string filePath = null)
        {
            return LogBlockDuration(methodName, logLevel);
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
