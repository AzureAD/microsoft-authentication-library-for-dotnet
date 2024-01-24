// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    internal static class EnvUtils
    {
        internal const string TraceLevelEnvVarName = "IDENTITYEXTENSIONTRACELEVEL";
        private const string DefaultTraceSource = "Microsoft.Identity.Client.Extensions.TraceSource";

        internal static TraceSource GetNewTraceSource(string sourceName)
        {
            sourceName = sourceName ?? DefaultTraceSource;
#if DEBUG
            var level = SourceLevels.Verbose;
#else
            var level = SourceLevels.Warning;
#endif
            string traceSourceLevelEnvVar = Environment.GetEnvironmentVariable(TraceLevelEnvVarName);
            if (!string.IsNullOrEmpty(traceSourceLevelEnvVar) &&
                Enum.TryParse(traceSourceLevelEnvVar, ignoreCase: true, result: out SourceLevels result))
            {
                level = result;
            }

            return new TraceSource(sourceName, level);
        }
    }
}
